using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,Warehouse")]
public class SupplierClaimsController : CrudControllerBase
{
    private static readonly string[] ActiveCustomerClaimStatuses =
    {
        "Open",
        "Received",
        "SentToSupplier",
        "Repairing",
        "ReadyToReturn"
    };

    private readonly AccountingDbContext _context;

    public SupplierClaimsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.SerialClaimLogs
            .AsNoTracking()
            .Include(x => x.CustomerClaimHeader)
            .Include(x => x.Supplier)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                (x.ProblemDescription != null && x.ProblemDescription.Contains(keyword)) ||
                (x.Remark != null && x.Remark.Contains(keyword)) ||
                (x.Supplier != null && (
                    x.Supplier.SupplierCode.Contains(keyword) ||
                    x.Supplier.SupplierName.Contains(keyword) ||
                    (x.Supplier.TaxId != null && x.Supplier.TaxId.Contains(keyword)))) ||
                (x.SerialNumber != null && (
                    x.SerialNumber.SerialNo.Contains(keyword) ||
                    (x.SerialNumber.Item != null && (
                        x.SerialNumber.Item.ItemCode.Contains(keyword) ||
                        x.SerialNumber.Item.ItemName.Contains(keyword) ||
                        (x.SerialNumber.Item.PartNumber != null && x.SerialNumber.Item.PartNumber.Contains(keyword)))))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.ClaimStatus == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.ClaimDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.ClaimDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var claims = await PaginatedList<SerialClaimLog>.CreateAsync(query
            .OrderByDescending(x => x.ClaimDate)
            .ThenByDescending(x => x.SerialClaimLogId), page, pageSize);

        return View(claims);
    }

    public async Task<IActionResult> Create(int? serialId)
    {
        if (!serialId.HasValue)
        {
            TempData["ClaimError"] = "Open the claim page from a selected serial.";
            return RedirectToAction("Index", "SerialInquiry");
        }

        var serial = await GetSerialAsync(serialId.Value, trackChanges: false);
        if (serial is null)
        {
            return NotFound();
        }

        var model = BuildFormModel(serial);
        PopulateStatusOptions(model);
        ApplyClaimBlockState(model, serial);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierClaimFormViewModel model)
    {
        if (!model.SerialId.HasValue)
        {
            return NotFound();
        }

        var serial = await GetSerialAsync(model.SerialId.Value, trackChanges: true);
        if (serial is null)
        {
            return NotFound();
        }

        PopulateSerialSummary(model, serial);
        PopulateStatusOptions(model);
        ValidateClaim(model, serial);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var claimLog = new SerialClaimLog
        {
            SerialId = serial.SerialId,
            SupplierId = serial.SupplierId!.Value,
            ClaimDate = model.ClaimDate.Date,
            ProblemDescription = string.IsNullOrWhiteSpace(model.ProblemDescription) ? null : model.ProblemDescription.Trim(),
            ClaimStatus = model.ClaimStatus,
            Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        _context.SerialClaimLogs.Add(claimLog);
        serial.Status = "ClaimedToSupplier";

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = claimLog.SerialClaimLogId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var claim = await _context.SerialClaimLogs
            .AsNoTracking()
            .Include(x => x.CustomerClaimHeader)
                .ThenInclude(x => x!.CustomerClaimDetails)
            .Include(x => x.Supplier)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .Include(x => x.SupplierReplacementSerialNumber)
            .FirstOrDefaultAsync(x => x.SerialClaimLogId == id.Value);

        return claim is null ? NotFound() : View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MarkSent(int id, DateTime? sentDate)
    {
        return ApplySimpleWorkflowAsync(
            id,
            new[] { "Open" },
            "Sent",
            claim =>
            {
                claim.SentDate = sentDate?.Date ?? DateTime.Today;
                claim.UpdatedDate = DateTime.UtcNow;
                if (claim.SerialNumber is not null)
                {
                    claim.SerialNumber.Status = "ClaimedToSupplier";
                }
            },
            "Supplier claim was marked as sent.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveRepairedOriginal(int id, DateTime? receivedDate, string? remark)
    {
        var claim = await GetTrackedClaimAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.ClaimStatus != "Open" && claim.ClaimStatus != "Sent")
        {
            TempData["ClaimNotice"] = "Receive repaired original is available only for Open or Sent supplier claims.";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        if (claim.SerialNumber is null)
        {
            return NotFound();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var actionDate = receivedDate?.Date ?? DateTime.Today;
            claim.ClaimStatus = "Returned";
            claim.ResultType = "RepairedOriginal";
            claim.ReceivedDate = actionDate;
            claim.UpdatedDate = now;
            AppendRemark(claim, remark);

            if (claim.CustomerClaimHeader is not null && ActiveCustomerClaimStatuses.Contains(claim.CustomerClaimHeader.Status))
            {
                    claim.SerialNumber.Status = HasCustomerReplacement(claim.CustomerClaimHeader) ? "InStock" : "CustomerClaim";
                    if (!HasCustomerReplacement(claim.CustomerClaimHeader))
                    {
                    claim.CustomerClaimHeader.Status = "Open";
                    claim.CustomerClaimHeader.ResolvedDate ??= actionDate;
                    claim.CustomerClaimHeader.UpdatedDate = now;
                    claim.CustomerClaimHeader.ResolutionRemark ??= "Original serial repaired by supplier and ready to return.";
                }
            }
            else
            {
                claim.SerialNumber.Status = "InStock";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["ClaimNotice"] = "Supplier repaired original serial was received.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ClaimNotice"] = "Receive repaired original failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceiveReplacement(int id, string? replacementSerialNo, DateTime? receivedDate, DateTime? supplierWarrantyStartDate, DateTime? supplierWarrantyEndDate, string? remark)
    {
        var claim = await GetTrackedClaimAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.ClaimStatus != "Open" && claim.ClaimStatus != "Sent")
        {
            TempData["ClaimNotice"] = "Receive replacement is available only for Open or Sent supplier claims.";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        if (claim.SerialNumber is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(replacementSerialNo))
        {
            TempData["ClaimNotice"] = "Replacement serial no. is required.";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        var serialNo = replacementSerialNo.Trim();
        if (await _context.SerialNumbers.AnyAsync(x => x.SerialNo == serialNo))
        {
            TempData["ClaimNotice"] = "Replacement serial no. already exists.";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        if (supplierWarrantyStartDate.HasValue && supplierWarrantyEndDate.HasValue &&
            supplierWarrantyEndDate.Value.Date < supplierWarrantyStartDate.Value.Date)
        {
            TempData["ClaimNotice"] = "Supplier warranty end date cannot be earlier than start date.";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var actionDate = receivedDate?.Date ?? DateTime.Today;
            var replacement = new SerialNumber
            {
                ItemId = claim.SerialNumber.ItemId,
                SerialNo = serialNo,
                Status = "InStock",
                SupplierId = claim.SupplierId,
                SupplierWarrantyStartDate = supplierWarrantyStartDate?.Date ?? DateTime.Today,
                SupplierWarrantyEndDate = supplierWarrantyEndDate?.Date,
                CreatedDate = now
            };

            _context.SerialNumbers.Add(replacement);
            claim.SerialNumber.Status = "Replaced";
            claim.ClaimStatus = "Replaced";
            claim.ResultType = "SupplierReplacement";
            claim.ReceivedDate = actionDate;
            claim.UpdatedDate = now;
            claim.SupplierReplacementSerialNumber = replacement;
            AppendRemark(claim, remark);

            if (claim.SerialNumber.Item is not null && claim.SerialNumber.Item.TrackStock)
            {
                claim.SerialNumber.Item.CurrentStock += 1;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["ClaimNotice"] = "Supplier replacement serial was received into stock.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ClaimNotice"] = "Receive replacement failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SupplierRejected(int id, DateTime? rejectedDate, string? remark)
    {
        return ApplySimpleWorkflowAsync(
            id,
            new[] { "Open", "Sent" },
            "Rejected",
            claim =>
            {
                claim.ResultType = "Rejected";
                claim.ReceivedDate = rejectedDate?.Date ?? DateTime.Today;
                claim.UpdatedDate = DateTime.UtcNow;
                AppendRemark(claim, remark);
                if (claim.SerialNumber is not null)
                {
                    claim.SerialNumber.Status = "Defective";
                }
            },
            "Supplier claim was rejected and original serial was marked Defective.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Close(int id, DateTime? closedDate)
    {
        return ApplySimpleWorkflowAsync(
            id,
            new[] { "Returned", "Replaced", "Rejected" },
            "Closed",
            claim =>
            {
                claim.ClosedDate = closedDate?.Date ?? DateTime.Today;
                claim.UpdatedDate = DateTime.UtcNow;
            },
            "Supplier claim was closed.");
    }

    private async Task<SerialNumber?> GetSerialAsync(int serialId, bool trackChanges)
    {
        var query = _context.SerialNumbers
            .Include(x => x.Item)
            .Include(x => x.Supplier)
            .Where(x => x.SerialId == serialId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private Task<SerialClaimLog?> GetTrackedClaimAsync(int id)
    {
        return _context.SerialClaimLogs
            .Include(x => x.CustomerClaimHeader)
                .ThenInclude(x => x!.CustomerClaimDetails)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .FirstOrDefaultAsync(x => x.SerialClaimLogId == id);
    }

    private async Task<IActionResult> ApplySimpleWorkflowAsync(
        int id,
        IReadOnlyCollection<string> allowedStatuses,
        string nextStatus,
        Action<SerialClaimLog> applyChanges,
        string successMessage)
    {
        var claim = await GetTrackedClaimAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (!allowedStatuses.Contains(claim.ClaimStatus))
        {
            TempData["ClaimNotice"] = $"Action is not available while supplier claim status is {claim.ClaimStatus}.";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            claim.ClaimStatus = nextStatus;
            applyChanges(claim);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["ClaimNotice"] = successMessage;
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ClaimNotice"] = "Supplier claim workflow action failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
    }

    private static bool HasCustomerReplacement(CustomerClaimHeader claim)
    {
        return claim.CustomerClaimDetails.Any(x => x.ReplacementSerialId.HasValue);
    }

    private static void AppendRemark(SerialClaimLog claim, string? remark)
    {
        if (string.IsNullOrWhiteSpace(remark))
        {
            return;
        }

        var trimmed = remark.Trim();
        claim.Remark = string.IsNullOrWhiteSpace(claim.Remark)
            ? trimmed
            : $"{claim.Remark}{Environment.NewLine}{trimmed}";
    }

    private static SupplierClaimFormViewModel BuildFormModel(SerialNumber serial)
    {
        var model = new SupplierClaimFormViewModel();
        PopulateSerialSummary(model, serial);
        model.ClaimStatus = "Open";
        return model;
    }

    private static void PopulateSerialSummary(SupplierClaimFormViewModel model, SerialNumber serial)
    {
        model.SerialId = serial.SerialId;
        model.SerialNo = serial.SerialNo;
        model.ItemCode = serial.Item?.ItemCode ?? string.Empty;
        model.ItemName = serial.Item?.ItemName ?? string.Empty;
        model.PartNumber = serial.Item?.PartNumber ?? string.Empty;
        model.SupplierName = serial.Supplier?.SupplierName ?? string.Empty;
        model.CurrentSerialStatus = serial.Status;
        model.SupplierWarrantyStartDate = serial.SupplierWarrantyStartDate;
        model.SupplierWarrantyEndDate = serial.SupplierWarrantyEndDate;
    }

    private static void ApplyClaimBlockState(SupplierClaimFormViewModel model, SerialNumber serial)
    {
        if (!serial.SupplierWarrantyEndDate.HasValue)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty is missing. Claim creation is not allowed for this serial.";
            return;
        }

        if (serial.SupplierWarrantyEndDate.Value.Date < DateTime.Today)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty has expired. Claim creation is not allowed for this serial.";
        }
    }

    private static void PopulateStatusOptions(SupplierClaimFormViewModel model)
    {
        model.StatusOptions = new[]
        {
            new SelectListItem("Open", "Open"),
            new SelectListItem("Sent", "Sent"),
            new SelectListItem("Returned", "Returned"),
            new SelectListItem("Rejected", "Rejected"),
            new SelectListItem("Closed", "Closed")
        };
    }

    private void ValidateClaim(SupplierClaimFormViewModel model, SerialNumber serial)
    {
        if (!serial.SupplierId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Selected serial does not have a supplier.");
        }

        if (!serial.SupplierWarrantyEndDate.HasValue)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty is missing. Claim creation is not allowed for this serial.";
            ModelState.AddModelError(string.Empty, model.ClaimBlockMessage);
            return;
        }

        if (serial.SupplierWarrantyEndDate.Value.Date < DateTime.Today)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "Supplier warranty has expired. Claim creation is not allowed for this serial.";
            ModelState.AddModelError(string.Empty, model.ClaimBlockMessage);
        }

        if (serial.SupplierWarrantyStartDate.HasValue && model.ClaimDate.Date < serial.SupplierWarrantyStartDate.Value.Date)
        {
            ModelState.AddModelError(nameof(model.ClaimDate), "Claim date cannot be earlier than the supplier warranty start date.");
        }

        if (model.ClaimDate.Date > serial.SupplierWarrantyEndDate.Value.Date)
        {
            ModelState.AddModelError(nameof(model.ClaimDate), "Claim date must be within the supplier warranty period.");
        }
    }
}
