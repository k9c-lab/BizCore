using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
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
            .Include(x => x.Branch)
            .Include(x => x.CustomerClaimHeader)
            .Include(x => x.Supplier)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .AsQueryable();

        if (!CurrentUserCanAccessAllBranches())
        {
            var branchId = CurrentBranchId();
            query = query.Where(x => x.BranchId == branchId);
        }

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
            TempData["ClaimError"] = "กรุณาเปิดหน้าเคลมจาก Serial ที่เลือก";
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
            BranchId = serial.BranchId,
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
            .Include(x => x.Branch)
            .Include(x => x.CustomerClaimHeader)
                .ThenInclude(x => x!.CustomerClaimDetails)
            .Include(x => x.Supplier)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .Include(x => x.SupplierReplacementSerialNumber)
            .FirstOrDefaultAsync(x => x.SerialClaimLogId == id.Value);

        return claim is null || !CanAccessBranch(claim.BranchId) ? NotFound() : View(claim);
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
            TempData["ClaimNotice"] = "รับของซ่อมกลับได้เฉพาะเคลมผู้ขายสถานะเปิดหรือส่งแล้ว";
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
                var customerAlreadyReceivedReplacement = HasCustomerReplacement(claim.CustomerClaimHeader);
                claim.SerialNumber.Status = customerAlreadyReceivedReplacement ? "InStock" : "CustomerClaim";
                if (!customerAlreadyReceivedReplacement)
                {
                    claim.CustomerClaimHeader.Status = "ReadyToReturn";
                    claim.CustomerClaimHeader.ResolvedDate ??= actionDate;
                    claim.CustomerClaimHeader.UpdatedDate = now;
                    claim.CustomerClaimHeader.ResolutionRemark ??= "Original serial repaired by supplier and ready to return.";
                }
                else if (claim.SerialNumber.Item?.TrackStock == true)
                {
                    claim.SerialNumber.Item.CurrentStock += 1;
                    await AdjustStockBalanceAsync(claim.SerialNumber.BranchId ?? claim.BranchId, claim.SerialNumber.ItemId, 1m);
                    AddStockMovement(
                        claim,
                        claim.SerialNumber.ItemId,
                        claim.SerialNumber.SerialId,
                        claim.SerialNumber.BranchId ?? claim.BranchId,
                        1m,
                        "SupplierRepairReturn",
                        "Supplier repaired original returned to stock after customer replacement.");
                }
            }
            else
            {
                claim.SerialNumber.Status = "InStock";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["ClaimNotice"] = "รับ Serial เดิมที่ซ่อมแล้วกลับเรียบร้อย";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ClaimNotice"] = "รับของซ่อมกลับไม่สำเร็จ ระบบไม่ได้บันทึกการเปลี่ยนแปลง";
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
            TempData["ClaimNotice"] = "รับสินค้าทดแทนได้เฉพาะเคลมผู้ขายสถานะเปิดหรือส่งแล้ว";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        if (claim.SerialNumber is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(replacementSerialNo))
        {
            TempData["ClaimNotice"] = "กรุณาระบุหมายเลข Serial ทดแทน";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        var serialNo = replacementSerialNo.Trim();
        if (await _context.SerialNumbers.AnyAsync(x => x.SerialNo == serialNo))
        {
            TempData["ClaimNotice"] = "หมายเลข Serial ทดแทนนี้มีอยู่ในระบบแล้ว";
            return RedirectToAction(nameof(Details), new { id = claim.SerialClaimLogId });
        }

        if (supplierWarrantyStartDate.HasValue && supplierWarrantyEndDate.HasValue &&
            supplierWarrantyEndDate.Value.Date < supplierWarrantyStartDate.Value.Date)
        {
            TempData["ClaimNotice"] = "วันสิ้นสุดประกันผู้ขายต้องไม่ก่อนวันเริ่มต้น";
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
                BranchId = claim.BranchId ?? claim.SerialNumber.BranchId,
                SupplierWarrantyStartDate = supplierWarrantyStartDate?.Date,
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
                await AdjustStockBalanceAsync(replacement.BranchId, replacement.ItemId, 1m);
                AddStockMovement(
                    claim,
                    replacement.ItemId,
                    null,
                    replacement.BranchId,
                    1m,
                    "SupplierReplacement",
                    $"Supplier replacement serial {replacement.SerialNo} received into stock.",
                    replacement);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["ClaimNotice"] = "รับ Serial ทดแทนจากผู้ขายเข้าสต็อกเรียบร้อย";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ClaimNotice"] = "รับสินค้าทดแทนไม่สำเร็จ ระบบไม่ได้บันทึกการเปลี่ยนแปลง";
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
            "ผู้ขายปฏิเสธเคลมเรียบร้อย และกำหนดสถานะ Serial เดิมเป็นชำรุด");
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
            "ปิดเคลมผู้ขายเรียบร้อยแล้ว");
    }

    private async Task<SerialNumber?> GetSerialAsync(int serialId, bool trackChanges)
    {
        var query = _context.SerialNumbers
            .Include(x => x.Item)
            .Include(x => x.Supplier)
            .Where(x => x.SerialId == serialId);

        if (!CurrentUserCanAccessAllBranches())
        {
            var branchId = CurrentBranchId();
            query = query.Where(x => x.BranchId == branchId);
        }

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private Task<SerialClaimLog?> GetTrackedClaimAsync(int id)
    {
        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        var branchId = CurrentBranchId();
        return _context.SerialClaimLogs
            .Include(x => x.CustomerClaimHeader)
                .ThenInclude(x => x!.CustomerClaimDetails)
            .Include(x => x.SerialNumber)
                .ThenInclude(x => x!.Item)
            .FirstOrDefaultAsync(x => x.SerialClaimLogId == id &&
                (canAccessAllBranches || x.BranchId == branchId));
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
            TempData["ClaimNotice"] = $"ไม่สามารถทำรายการนี้ได้ในสถานะเคลมผู้ขาย {claim.ClaimStatus}";
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
            TempData["ClaimNotice"] = "ทำรายการ workflow เคลมผู้ขายไม่สำเร็จ ระบบไม่ได้บันทึกการเปลี่ยนแปลง";
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

    private async Task AdjustStockBalanceAsync(int? branchId, int itemId, decimal qtyDelta)
    {
        if (!branchId.HasValue || qtyDelta == 0)
        {
            return;
        }

        var balance = await _context.StockBalances
            .FirstOrDefaultAsync(x => x.BranchId == branchId.Value && x.ItemId == itemId);

        if (balance is null)
        {
            balance = new StockBalance
            {
                BranchId = branchId.Value,
                ItemId = itemId,
                QtyOnHand = 0
            };
            _context.StockBalances.Add(balance);
        }

        balance.QtyOnHand += qtyDelta;
    }

    private void AddStockMovement(
        SerialClaimLog claim,
        int itemId,
        int? serialId,
        int? toBranchId,
        decimal qty,
        string movementType,
        string remark,
        SerialNumber? serialNumber = null)
    {
        _context.StockMovements.Add(new StockMovement
        {
            MovementDate = claim.ReceivedDate?.Date ?? DateTime.Today,
            MovementType = movementType,
            ReferenceType = "SupplierClaim",
            ReferenceId = claim.SerialClaimLogId,
            ItemId = itemId,
            SerialId = serialId,
            SerialNumber = serialNumber,
            ToBranchId = toBranchId,
            Qty = qty,
            Remark = remark,
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        });
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
        if (serial.SupplierWarrantyEndDate.HasValue && serial.SupplierWarrantyEndDate.Value.Date < DateTime.Today)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "ประกันผู้ขายหมดอายุแล้ว ไม่สามารถสร้างเคลมสำหรับ Serial นี้ได้";
        }
    }

    private static void PopulateStatusOptions(SupplierClaimFormViewModel model)
    {
        model.StatusOptions = new[]
        {
            new SelectListItem("เปิด", "Open"),
            new SelectListItem("ส่งแล้ว", "Sent"),
            new SelectListItem("รับกลับแล้ว", "Returned"),
            new SelectListItem("ปฏิเสธ", "Rejected"),
            new SelectListItem("ปิดเคลม", "Closed")
        };
    }

    private void ValidateClaim(SupplierClaimFormViewModel model, SerialNumber serial)
    {
        if (!CanAccessBranch(serial.BranchId))
        {
            ModelState.AddModelError(string.Empty, "คุณไม่มีสิทธิ์สร้างเคลมผู้ขายให้สาขาอื่น");
        }

        if (!serial.SupplierId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Serial ที่เลือกยังไม่ผูกกับผู้ขาย");
        }

        if (serial.SupplierWarrantyEndDate.HasValue && serial.SupplierWarrantyEndDate.Value.Date < DateTime.Today)
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "ประกันผู้ขายหมดอายุแล้ว ไม่สามารถสร้างเคลมสำหรับ Serial นี้ได้";
            ModelState.AddModelError(string.Empty, model.ClaimBlockMessage);
        }

        if (serial.SupplierWarrantyStartDate.HasValue && model.ClaimDate.Date < serial.SupplierWarrantyStartDate.Value.Date)
        {
            ModelState.AddModelError(nameof(model.ClaimDate), "วันที่เคลมห้ามก่อนวันเริ่มประกันผู้ขาย");
        }

        if (serial.SupplierWarrantyEndDate.HasValue && model.ClaimDate.Date > serial.SupplierWarrantyEndDate.Value.Date)
        {
            ModelState.AddModelError(nameof(model.ClaimDate), "วันที่เคลมต้องอยู่ในช่วงประกันผู้ขาย");
        }
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }
}
