using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,BranchAdmin,Warehouse")]
public class CustomerClaimsController : CrudControllerBase
{
    private const string NumberPrefix = "CC";
    private static readonly string[] OpenStatuses =
    {
        "Open",
        "Received",
        "SentToSupplier",
        "Repairing",
        "ReadyToReturn",
        "ReturnedToCustomer"
    };
    private static readonly string[] CancelAllowedStatuses = { "Open", "Received", "Repairing", "ReadyToReturn" };
    private static readonly string[] RepairCompleteAllowedStatuses = { "Received", "Repairing" };
    private static readonly string[] RejectAllowedStatuses = { "Open", "Received", "Repairing", "SentToSupplier", "ReadyToReturn" };
    private static readonly string[] ActiveSupplierClaimStatuses = { "Open", "Sent" };

    private readonly AccountingDbContext _context;

    public CustomerClaimsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.CustomerClaimHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.InvoiceHeader)
            .Include(x => x.CustomerClaimDetails)
                .ThenInclude(x => x.SerialNumber)
            .Include(x => x.CustomerClaimDetails)
                .ThenInclude(x => x.Item)
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
                x.CustomerClaimNo.Contains(keyword) ||
                (x.ProblemDescription != null && x.ProblemDescription.Contains(keyword)) ||
                (x.Customer != null && (
                    x.Customer.CustomerCode.Contains(keyword) ||
                    x.Customer.CustomerName.Contains(keyword) ||
                    (x.Customer.TaxId != null && x.Customer.TaxId.Contains(keyword)))) ||
                (x.InvoiceHeader != null && x.InvoiceHeader.InvoiceNo.Contains(keyword)) ||
                x.CustomerClaimDetails.Any(d =>
                    d.SerialNumber != null && d.SerialNumber.SerialNo.Contains(keyword) ||
                    d.Item != null && (
                        d.Item.ItemCode.Contains(keyword) ||
                        d.Item.ItemName.Contains(keyword) ||
                        d.Item.PartNumber.Contains(keyword))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CustomerClaimDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.CustomerClaimDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var claims = await PaginatedList<CustomerClaimHeader>.CreateAsync(query
            .OrderByDescending(x => x.CustomerClaimDate)
            .ThenByDescending(x => x.CustomerClaimId), page, pageSize);

        return View(claims);
    }

    public async Task<IActionResult> Create(int? serialId)
    {
        if (!serialId.HasValue)
        {
            TempData["CustomerClaimNotice"] = "Open customer claim from a selected sold serial.";
            return RedirectToAction("Index", "SerialInquiry");
        }

        var serial = await GetSerialAsync(serialId.Value, trackChanges: false);
        if (serial is null)
        {
            return NotFound();
        }

        var model = await BuildFormModelAsync(serial);
        ApplyClaimBlockState(model, serial);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerClaimFormViewModel model)
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

        await PopulateSerialSummaryAsync(model, serial);
        await ValidateClaimAsync(model, serial);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var claim = new CustomerClaimHeader
            {
                CustomerClaimNo = await GetNextCustomerClaimNumberAsync(model.CustomerClaimDate),
                CustomerClaimDate = model.CustomerClaimDate.Date,
                CustomerId = serial.CurrentCustomerId!.Value,
                InvoiceId = serial.InvoiceId,
                BranchId = serial.BranchId,
                Status = "Open",
                ProblemDescription = string.IsNullOrWhiteSpace(model.ProblemDescription) ? null : model.ProblemDescription.Trim(),
                CreatedByUserId = userId,
                CreatedDate = now,
                CustomerClaimDetails = new List<CustomerClaimDetail>
                {
                    new()
                    {
                        SerialId = serial.SerialId,
                        ItemId = serial.ItemId,
                        OriginalInvoiceId = serial.InvoiceId,
                        LineRemark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim()
                    }
                }
            };

            _context.CustomerClaimHeaders.Add(claim);
            serial.Status = "CustomerClaim";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Customer claim number is already in use. Please try again.");
        }

        return View(model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var claim = await _context.CustomerClaimHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.InvoiceHeader)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ReceivedByUser)
            .Include(x => x.SentToSupplierByUser)
            .Include(x => x.ResolvedByUser)
            .Include(x => x.ReturnedByUser)
            .Include(x => x.ClosedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.CustomerClaimDetails)
                .ThenInclude(x => x.SerialNumber)
            .Include(x => x.CustomerClaimDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.CustomerClaimDetails)
                .ThenInclude(x => x.ReplacementSerialNumber)
            .FirstOrDefaultAsync(x => x.CustomerClaimId == id.Value);

        if (claim is null || !CanAccessBranch(claim.BranchId))
        {
            return NotFound();
        }

        await PopulatePhase3ViewDataAsync(claim);
        return View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ReceiveItem(int id)
    {
        return ApplyWorkflowActionAsync(
            id,
            new[] { "Open" },
            "Received",
            "CustomerClaim",
            "Customer claim item was received.",
            claim =>
            {
                var now = DateTime.UtcNow;
                var userId = CurrentUserId();
                claim.ReceivedByUserId = userId;
                claim.ReceivedDate = now;
                SetUpdatedAudit(claim, userId, now);
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToSupplier(int id, DateTime? sentToSupplierDate)
    {
        var claim = await GetTrackedClaimWithSerialAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != "Received" && claim.Status != "Repairing")
        {
            TempData["CustomerClaimNotice"] = "Receive the customer item before sending it to supplier.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var serial = GetClaimSerial(claim);
        if (serial is null)
        {
            return NotFound();
        }

        var actionDate = sentToSupplierDate?.Date ?? DateTime.Today;
        var supplierBlockedReason = GetSendToSupplierBlockedReason(serial, actionDate);
        if (!string.IsNullOrWhiteSpace(supplierBlockedReason))
        {
            TempData["CustomerClaimNotice"] = supplierBlockedReason;
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        if (await _context.SerialClaimLogs.AnyAsync(x => x.CustomerClaimId == claim.CustomerClaimId))
        {
            TempData["CustomerClaimNotice"] = "This customer claim is already linked to a supplier claim.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        if (await _context.SerialClaimLogs.AnyAsync(x => x.SerialId == serial.SerialId && ActiveSupplierClaimStatuses.Contains(x.ClaimStatus)))
        {
            TempData["CustomerClaimNotice"] = "This serial already has an active supplier claim.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var supplierClaim = new SerialClaimLog
            {
                SerialId = serial.SerialId,
                SupplierId = serial.SupplierId!.Value,
                CustomerClaimId = claim.CustomerClaimId,
                BranchId = claim.BranchId ?? serial.BranchId,
                ClaimDate = actionDate,
                ProblemDescription = claim.ProblemDescription,
                ClaimStatus = "Sent",
                SentDate = actionDate,
                Remark = $"Created from customer claim {claim.CustomerClaimNo}.",
                CreatedDate = now,
                UpdatedDate = now
            };

            _context.SerialClaimLogs.Add(supplierClaim);
            claim.Status = "SentToSupplier";
            claim.SentToSupplierByUserId = userId;
            claim.SentToSupplierDate = actionDate;
            SetUpdatedAudit(claim, userId, now);
            serial.Status = "ClaimedToSupplier";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CustomerClaimNotice"] = "Supplier claim was created and marked as sent.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["CustomerClaimNotice"] = "Send to supplier failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RepairComplete(int id, string? resolutionRemark)
    {
        return ApplyWorkflowActionAsync(
            id,
            RepairCompleteAllowedStatuses,
            "ReadyToReturn",
            "CustomerClaim",
            "Customer claim was marked ready to return.",
            claim =>
            {
                var now = DateTime.UtcNow;
                var userId = CurrentUserId();
                claim.ResolvedByUserId = userId;
                claim.ResolvedDate = now;
                if (!string.IsNullOrWhiteSpace(resolutionRemark))
                {
                    claim.ResolutionRemark = resolutionRemark.Trim();
                }
                SetUpdatedAudit(claim, userId, now);
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RejectClaim(int id, string? resolutionRemark)
    {
        return ApplyWorkflowActionAsync(
            id,
            RejectAllowedStatuses,
            "Rejected",
            "Sold",
            "Customer claim was rejected and serial status was restored to Sold.",
            claim =>
            {
                if (HasReplacementSerial(claim))
                {
                    throw new InvalidOperationException("Rejected claim cannot already have a replacement serial.");
                }

                var now = DateTime.UtcNow;
                var userId = CurrentUserId();
                claim.ResolvedByUserId = userId;
                claim.ResolvedDate = now;
                if (!string.IsNullOrWhiteSpace(resolutionRemark))
                {
                    claim.ResolutionRemark = resolutionRemark.Trim();
                }
                SetUpdatedAudit(claim, userId, now);
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ReturnToCustomer(int id, DateTime? returnedDate, string? resolutionRemark)
    {
        return ReturnToCustomerAsync(id, returnedDate, resolutionRemark);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CloseClaim(int id, DateTime? closedDate)
    {
        return CloseClaimAsync(id, closedDate);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplierClaim(int id)
    {
        var claim = await GetTrackedClaimWithSerialAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != "Received" && claim.Status != "Repairing")
        {
            TempData["CustomerClaimNotice"] = "Supplier claim can be created only after the customer item has been received.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        if (await _context.SerialClaimLogs.AnyAsync(x => x.CustomerClaimId == claim.CustomerClaimId))
        {
            TempData["CustomerClaimNotice"] = "This customer claim is already linked to a supplier claim.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var serial = GetClaimSerial(claim);
        if (serial is null)
        {
            return NotFound();
        }

        var actionDate = DateTime.Today;
        var supplierBlockedReason = GetSendToSupplierBlockedReason(serial, actionDate);
        if (!string.IsNullOrWhiteSpace(supplierBlockedReason))
        {
            TempData["CustomerClaimNotice"] = supplierBlockedReason;
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        if (await _context.SerialClaimLogs.AnyAsync(x => x.SerialId == serial.SerialId && ActiveSupplierClaimStatuses.Contains(x.ClaimStatus)))
        {
            TempData["CustomerClaimNotice"] = "This serial already has an active supplier claim.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var supplierClaim = new SerialClaimLog
            {
                SerialId = serial.SerialId,
                SupplierId = serial.SupplierId!.Value,
                CustomerClaimId = claim.CustomerClaimId,
                BranchId = claim.BranchId ?? serial.BranchId,
                ClaimDate = actionDate,
                ProblemDescription = claim.ProblemDescription,
                ClaimStatus = "Open",
                Remark = $"Created from customer claim {claim.CustomerClaimNo}.",
                CreatedDate = now
            };

            _context.SerialClaimLogs.Add(supplierClaim);
            claim.Status = "SentToSupplier";
            claim.SentToSupplierByUserId = userId;
            claim.SentToSupplierDate ??= now;
            SetUpdatedAudit(claim, userId, now);
            serial.Status = "ClaimedToSupplier";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CustomerClaimNotice"] = "Supplier claim was created and linked to this customer claim.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["CustomerClaimNotice"] = "Supplier claim creation failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignReplacement(int id, int? replacementSerialId)
    {
        if (!replacementSerialId.HasValue)
        {
            TempData["CustomerClaimNotice"] = "Select a replacement serial before assigning replacement.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var claim = await _context.CustomerClaimHeaders
            .Include(x => x.CustomerClaimDetails)
                .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.CustomerClaimId == id);

        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != "Received" && claim.Status != "Repairing" && claim.Status != "SentToSupplier" && claim.Status != "ReadyToReturn")
        {
            TempData["CustomerClaimNotice"] = "Replacement can be assigned only while the claim is active and before return to customer.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var detail = claim.CustomerClaimDetails.FirstOrDefault();
        if (detail is null || detail.SerialNumber is null)
        {
            return NotFound();
        }

        if (detail.ReplacementSerialId.HasValue)
        {
            TempData["CustomerClaimNotice"] = "This customer claim already has a replacement serial.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var replacement = await _context.SerialNumbers
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.SerialId == replacementSerialId.Value);
        if (replacement is null)
        {
            return NotFound();
        }

        var replacementBlockedReason = GetReplacementBlockedReason(detail, replacement);
        if (!string.IsNullOrWhiteSpace(replacementBlockedReason))
        {
            TempData["CustomerClaimNotice"] = replacementBlockedReason;
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();

            detail.ReplacementSerialId = replacement.SerialId;
            detail.SerialNumber.Status = "Replaced";
            replacement.Status = "Sold";
            replacement.CurrentCustomerId = claim.CustomerId;
            replacement.InvoiceId = claim.InvoiceId;
            replacement.CustomerWarrantyStartDate = detail.SerialNumber.CustomerWarrantyEndDate.HasValue ? DateTime.Today : null;
            replacement.CustomerWarrantyEndDate = detail.SerialNumber.CustomerWarrantyEndDate;
            if (replacement.Item?.TrackStock == true)
            {
                replacement.Item.CurrentStock -= 1;
                await AdjustStockBalanceAsync(replacement.BranchId, replacement.ItemId, -1m);
                AddStockMovement(
                    claim,
                    replacement.ItemId,
                    replacement.SerialId,
                    replacement.BranchId,
                    -1m,
                    "CustomerClaimReplacement",
                    $"Replacement serial {replacement.SerialNo} issued to customer.");
            }
            claim.Status = "ReadyToReturn";
            claim.ResolvedByUserId = userId;
            claim.ResolvedDate ??= now;
            claim.ResolutionRemark ??= $"Replacement serial {replacement.SerialNo} assigned.";
            SetUpdatedAudit(claim, userId, now);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CustomerClaimNotice"] = "Replacement serial was assigned and claim is ready to return.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["CustomerClaimNotice"] = "Replacement assignment failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var claim = await _context.CustomerClaimHeaders
            .Include(x => x.CustomerClaimDetails)
            .FirstOrDefaultAsync(x => x.CustomerClaimId == id);

        if (claim is null)
        {
            return NotFound();
        }

        if (!CancelAllowedStatuses.Contains(claim.Status))
        {
            TempData["CustomerClaimNotice"] = "Only open, received, repairing, or ready-to-return customer claims can be cancelled.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        if (HasReplacementSerial(claim))
        {
            TempData["CustomerClaimNotice"] = "Cancel claim is blocked because a replacement serial has already been assigned.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var serialId = claim.CustomerClaimDetails.Select(x => x.SerialId).FirstOrDefault();
        var serial = await _context.SerialNumbers.FirstOrDefaultAsync(x => x.SerialId == serialId);
        if (serial is null)
        {
            return NotFound();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            claim.Status = "Cancelled";
            claim.UpdatedByUserId = userId;
            claim.UpdatedDate = now;
            claim.CancelledByUserId = userId;
            claim.CancelledDate = now;
            claim.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason.Trim();
            serial.Status = "Sold";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CustomerClaimNotice"] = "Customer claim was cancelled and serial status was restored to Sold.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["CustomerClaimNotice"] = "Customer claim cancellation failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
    }

    private async Task<SerialNumber?> GetSerialAsync(int serialId, bool trackChanges)
    {
        var query = _context.SerialNumbers
            .Include(x => x.Item)
            .Include(x => x.CurrentCustomer)
            .Include(x => x.InvoiceHeader)
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

    private Task<CustomerClaimHeader?> GetTrackedClaimWithSerialAsync(int id)
    {
        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        var branchId = CurrentBranchId();
        return _context.CustomerClaimHeaders
            .Include(x => x.CustomerClaimDetails)
                .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.CustomerClaimId == id &&
                (canAccessAllBranches || x.BranchId == branchId));
    }

    private async Task PopulatePhase3ViewDataAsync(CustomerClaimHeader claim)
    {
        var detail = claim.CustomerClaimDetails.FirstOrDefault();
        if (detail is null)
        {
            ViewData["ReplacementSerialOptions"] = Enumerable.Empty<SelectListItem>();
            return;
        }

        var supplierClaim = await _context.SerialClaimLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerClaimId == claim.CustomerClaimId);

        ViewData["LinkedSupplierClaimId"] = supplierClaim?.SerialClaimLogId;
        ViewData["LinkedSupplierClaimStatus"] = supplierClaim?.ClaimStatus;

        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        var branchId = CurrentBranchId();
        var replacementOptions = await _context.SerialNumbers
            .AsNoTracking()
            .Where(x => x.ItemId == detail.ItemId &&
                x.Status == "InStock" &&
                (canAccessAllBranches || x.BranchId == branchId) &&
                (!claim.BranchId.HasValue || x.BranchId == claim.BranchId))
            .OrderBy(x => x.SerialNo)
            .Select(x => new SelectListItem($"{x.SerialNo}", x.SerialId.ToString()))
            .ToListAsync();

        ViewData["ReplacementSerialOptions"] = replacementOptions;
    }

    private async Task<IActionResult> ApplyWorkflowActionAsync(
        int id,
        IReadOnlyCollection<string> allowedStatuses,
        string nextStatus,
        string serialStatus,
        string successMessage,
        Action<CustomerClaimHeader> applyAudit)
    {
        var claim = await GetTrackedClaimWithSerialAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (!allowedStatuses.Contains(claim.Status))
        {
            TempData["CustomerClaimNotice"] = $"Action is not available while claim status is {claim.Status}.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var serial = GetClaimSerial(claim);
        if (serial is null)
        {
            return NotFound();
        }

        return await ApplyWorkflowActionAsync(claim, serial, nextStatus, serialStatus, successMessage, applyAudit);
    }

    private async Task<IActionResult> ApplyWorkflowActionAsync(
        CustomerClaimHeader claim,
        SerialNumber serial,
        string nextStatus,
        string serialStatus,
        string successMessage,
        Action<CustomerClaimHeader> applyAudit)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            applyAudit(claim);
            claim.Status = nextStatus;
            serial.Status = serialStatus;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CustomerClaimNotice"] = successMessage;
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["CustomerClaimNotice"] = "Customer claim workflow action failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
    }

    private async Task<IActionResult> ReturnToCustomerAsync(int id, DateTime? returnedDate, string? resolutionRemark)
    {
        var claim = await GetTrackedClaimWithSerialAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != "ReadyToReturn")
        {
            TempData["CustomerClaimNotice"] = "Return to customer is available only after the claim is ready to return.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var serial = GetClaimSerial(claim);
        if (serial is null)
        {
            return NotFound();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var actionDate = returnedDate?.Date ?? DateTime.Today;
            var userId = CurrentUserId();
            claim.Status = "ReturnedToCustomer";
            claim.ReturnedByUserId = userId;
            claim.ReturnedDate = actionDate;
            if (!string.IsNullOrWhiteSpace(resolutionRemark))
            {
                claim.ResolutionRemark = resolutionRemark.Trim();
            }
            SetUpdatedAudit(claim, userId, now);
            if (!HasReplacementSerial(claim))
            {
                serial.Status = "Sold";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CustomerClaimNotice"] = "Customer claim item was returned to customer.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["CustomerClaimNotice"] = "Customer claim workflow action failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
    }

    private async Task<IActionResult> CloseClaimAsync(int id, DateTime? closedDate)
    {
        var claim = await GetTrackedClaimWithSerialAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != "ReturnedToCustomer" && claim.Status != "Rejected")
        {
            TempData["CustomerClaimNotice"] = "Close claim is available only after return to customer or rejection.";
            return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
        }

        var serial = GetClaimSerial(claim);
        if (serial is null)
        {
            return NotFound();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var actionDate = closedDate?.Date ?? DateTime.Today;
            var userId = CurrentUserId();
            claim.Status = "Closed";
            claim.ClosedByUserId = userId;
            claim.ClosedDate = actionDate;
            SetUpdatedAudit(claim, userId, now);
            if (!HasReplacementSerial(claim))
            {
                serial.Status = "Sold";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CustomerClaimNotice"] = "Customer claim was closed.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["CustomerClaimNotice"] = "Customer claim workflow action failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = claim.CustomerClaimId });
    }

    private static SerialNumber? GetClaimSerial(CustomerClaimHeader claim)
    {
        return claim.CustomerClaimDetails.Select(x => x.SerialNumber).FirstOrDefault();
    }

    private static string? GetSendToSupplierBlockedReason(SerialNumber serial, DateTime sendDate)
    {
        if (!serial.SupplierId.HasValue)
        {
            return "Send to supplier is blocked because this serial is not linked to a supplier.";
        }

        if (!serial.SupplierWarrantyEndDate.HasValue)
        {
            return "Send to supplier is blocked because supplier warranty end date is missing.";
        }

        var actionDate = sendDate.Date;
        if (serial.SupplierWarrantyStartDate.HasValue && actionDate < serial.SupplierWarrantyStartDate.Value.Date)
        {
            return "Send to supplier is blocked because supplier warranty has not started.";
        }

        if (actionDate > serial.SupplierWarrantyEndDate.Value.Date)
        {
            return "Send to supplier is blocked because supplier warranty has expired.";
        }

        return null;
    }

    private static string? GetReplacementBlockedReason(CustomerClaimDetail detail, SerialNumber replacement)
    {
        if (replacement.SerialId == detail.SerialId)
        {
            return "Replacement serial cannot be the same serial as the claimed item.";
        }

        if (!string.Equals(replacement.Status, "InStock", StringComparison.OrdinalIgnoreCase))
        {
            return $"Replacement serial must be InStock. Current status is {replacement.Status}.";
        }

        if (replacement.ItemId != detail.ItemId)
        {
            return "Replacement serial must be the same item as the claimed serial.";
        }

        if (detail.SerialNumber is not null && replacement.BranchId != detail.SerialNumber.BranchId)
        {
            return "Replacement serial must belong to the same branch as the claimed serial.";
        }

        return null;
    }

    private static bool HasReplacementSerial(CustomerClaimHeader claim)
    {
        return claim.CustomerClaimDetails.Any(x => x.ReplacementSerialId.HasValue);
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
        CustomerClaimHeader claim,
        int itemId,
        int? serialId,
        int? fromBranchId,
        decimal qty,
        string movementType,
        string remark)
    {
        _context.StockMovements.Add(new StockMovement
        {
            MovementDate = claim.ResolvedDate?.Date ?? DateTime.Today,
            MovementType = movementType,
            ReferenceType = "CustomerClaim",
            ReferenceId = claim.CustomerClaimId,
            ItemId = itemId,
            SerialId = serialId,
            FromBranchId = fromBranchId,
            Qty = qty,
            Remark = remark,
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        });
    }

    private static void SetUpdatedAudit(CustomerClaimHeader claim, int? userId, DateTime now)
    {
        claim.UpdatedByUserId = userId;
        claim.UpdatedDate = now;
    }

    private async Task<CustomerClaimFormViewModel> BuildFormModelAsync(SerialNumber serial)
    {
        var model = new CustomerClaimFormViewModel
        {
            CustomerClaimNo = await GetNextCustomerClaimNumberAsync(DateTime.Today)
        };
        await PopulateSerialSummaryAsync(model, serial);
        return model;
    }

    private async Task PopulateSerialSummaryAsync(CustomerClaimFormViewModel model, SerialNumber serial)
    {
        model.SerialId = serial.SerialId;
        model.SerialNo = serial.SerialNo;
        model.ItemCode = serial.Item?.ItemCode ?? string.Empty;
        model.ItemName = serial.Item?.ItemName ?? string.Empty;
        model.PartNumber = serial.Item?.PartNumber ?? string.Empty;
        model.CustomerCode = serial.CurrentCustomer?.CustomerCode ?? string.Empty;
        model.CustomerName = serial.CurrentCustomer?.CustomerName ?? string.Empty;
        model.InvoiceNo = serial.InvoiceHeader?.InvoiceNo ?? string.Empty;
        model.CurrentSerialStatus = serial.Status;
        model.CustomerWarrantyStartDate = serial.CustomerWarrantyStartDate;
        model.CustomerWarrantyEndDate = serial.CustomerWarrantyEndDate;

        if (await HasOpenClaimAsync(serial.SerialId))
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "This serial already has an open customer claim.";
        }
    }

    private void ApplyClaimBlockState(CustomerClaimFormViewModel model, SerialNumber serial)
    {
        if (model.IsClaimBlocked)
        {
            return;
        }

        var reason = GetClaimBlockedReason(serial, DateTime.Today);
        if (!string.IsNullOrWhiteSpace(reason))
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = reason;
        }
    }

    private async Task ValidateClaimAsync(CustomerClaimFormViewModel model, SerialNumber serial)
    {
        var reason = GetClaimBlockedReason(serial, model.CustomerClaimDate);
        if (!string.IsNullOrWhiteSpace(reason))
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = reason;
            ModelState.AddModelError(string.Empty, reason);
        }

        if (await HasOpenClaimAsync(serial.SerialId))
        {
            model.IsClaimBlocked = true;
            model.ClaimBlockMessage = "This serial already has an open customer claim.";
            ModelState.AddModelError(string.Empty, model.ClaimBlockMessage);
        }
    }

    private static string? GetClaimBlockedReason(SerialNumber serial, DateTime claimDate)
    {
        if (!string.Equals(serial.Status, "Sold", StringComparison.OrdinalIgnoreCase))
        {
            return $"Customer claim is available only for Sold serials. Current status is {serial.Status}.";
        }

        if (!serial.CurrentCustomerId.HasValue)
        {
            return "Customer claim is blocked because this serial is not linked to a customer.";
        }

        if (!serial.InvoiceId.HasValue)
        {
            return "Customer claim is blocked because this serial is not linked to an invoice.";
        }

        if (serial.CustomerWarrantyStartDate.HasValue && claimDate.Date < serial.CustomerWarrantyStartDate.Value.Date)
        {
            return "Claim date cannot be earlier than the customer warranty start date.";
        }

        if (serial.CustomerWarrantyEndDate.HasValue && claimDate.Date > serial.CustomerWarrantyEndDate.Value.Date)
        {
            return "Customer warranty has expired. Claim creation is not allowed for this serial.";
        }

        return null;
    }

    private Task<bool> HasOpenClaimAsync(int serialId)
    {
        return _context.CustomerClaimDetails
            .AnyAsync(x => x.SerialId == serialId &&
                x.CustomerClaimHeader != null &&
                OpenStatuses.Contains(x.CustomerClaimHeader.Status));
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private Task<string> GetNextCustomerClaimNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.CustomerClaimHeaders.Select(x => x.CustomerClaimNo), prefix, date);
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
