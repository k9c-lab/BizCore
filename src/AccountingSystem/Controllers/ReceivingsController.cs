using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BizCore.Controllers;

[Authorize]
public class ReceivingsController : CrudControllerBase
{
    private const string NumberPrefix = "GR";
    private readonly AccountingDbContext _context;
    private readonly CompanyProfileSettings _companyProfile;

    public ReceivingsController(AccountingDbContext context, IOptions<CompanyProfileSettings> companyProfileOptions)
    {
        _context = context;
        _companyProfile = companyProfileOptions.Value;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        if (!CurrentUserHasPermission("Receiving.View"))
        {
            return Forbid();
        }

        var query = _context.ReceivingHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrderHeader)
            .Include(x => x.Branch)
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
                x.ReceivingNo.Contains(keyword) ||
                (x.Remark != null && x.Remark.Contains(keyword)) ||
                (x.PurchaseOrderHeader != null && x.PurchaseOrderHeader.PONo.Contains(keyword)) ||
                (x.Supplier != null && (
                    x.Supplier.SupplierCode.Contains(keyword) ||
                    x.Supplier.SupplierName.Contains(keyword) ||
                    (x.Supplier.TaxId != null && x.Supplier.TaxId.Contains(keyword)))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.ReceiveDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.ReceiveDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var receivings = await PaginatedList<ReceivingHeader>.CreateAsync(query
            .OrderByDescending(x => x.ReceiveDate)
            .ThenByDescending(x => x.ReceivingId), page, pageSize);

        return View(receivings);
    }

    public async Task<IActionResult> Create(int? purchaseOrderId, int? branchId)
    {
        if (!CurrentUserHasPermission("Receiving.Create"))
        {
            return Forbid();
        }

        var model = new ReceivingFormViewModel
        {
            ReceivingNo = await GetNextReceivingNumberAsync(DateTime.Today),
            BranchId = branchId
        };

        await PopulateLookupsAsync(model);
        if (purchaseOrderId.HasValue && model.PurchaseOrderLookup.Any(x => x.PurchaseOrderId == purchaseOrderId.Value))
        {
            PopulateDetailsFromLookup(model, purchaseOrderId.Value);
        }
        else if (purchaseOrderId.HasValue)
        {
            TempData["ReceivingNotice"] = "Selected purchase order is not available for receiving.";
            return RedirectToAction(nameof(Index), "PurchaseOrders");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceivingFormViewModel model, string command)
    {
        if (!CurrentUserHasPermission("Receiving.Create"))
        {
            return Forbid();
        }

        model.ReceivingNo = await EnsureReceivingNumberAsync(model.ReceivingNo, model.ReceiveDate);
        ModelState.Remove(nameof(ReceivingFormViewModel.ReceivingNo));
        var saveDraft = IsSaveDraftCommand(command);
        if (!saveDraft && !CurrentUserHasPermission("Receiving.Post"))
        {
            return Forbid();
        }

        EnsureBranchFromPostedAllocation(model);
        await PopulateLookupsAsync(model);
        if (saveDraft ? !await ValidateReceivingDraftAsync(model) : !await ValidateReceivingAsync(model))
        {
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var header = new ReceivingHeader
            {
                ReceivingNo = model.ReceivingNo,
                ReceiveDate = model.ReceiveDate,
                SupplierId = model.SupplierId!.Value,
                PurchaseOrderId = model.PurchaseOrderId!.Value,
                BranchId = model.BranchId,
                DeliveryNoteNo = model.DeliveryNoteNo?.Trim(),
                Remark = model.Remark?.Trim(),
                Status = saveDraft ? "Draft" : "Posted",
                CreatedByUserId = userId,
                PostedByUserId = saveDraft ? null : userId,
                PostedDate = saveDraft ? null : now,
                CreatedDate = now
            };

            _context.ReceivingHeaders.Add(header);
            await _context.SaveChangesAsync();

            await AddReceivingDetailsAsync(header.ReceivingId, model);
            if (!saveDraft)
            {
                await ApplyPostedReceivingAsync(header, model);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = header.ReceivingId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Receiving number or serial number must be unique.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (!CurrentUserHasPermission("Receiving.Edit"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var receiving = await GetReceivingForEditAsync(id.Value);
        if (receiving is null || !CanAccessBranch(receiving.BranchId))
        {
            return NotFound();
        }

        if (receiving.Status != "Draft")
        {
            TempData["ReceivingNotice"] = "Only Draft receiving documents can be edited.";
            return RedirectToAction(nameof(Details), new { id = receiving.ReceivingId });
        }

        var model = await BuildFormModelFromReceivingAsync(receiving);
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReceivingFormViewModel model, string command)
    {
        if (!CurrentUserHasPermission("Receiving.Edit"))
        {
            return Forbid();
        }

        var receiving = await GetReceivingForEditAsync(id);
        if (receiving is null || !CanAccessBranch(receiving.BranchId))
        {
            return NotFound();
        }

        if (receiving.Status != "Draft")
        {
            TempData["ReceivingNotice"] = "Only Draft receiving documents can be edited.";
            return RedirectToAction(nameof(Details), new { id = receiving.ReceivingId });
        }

        model.ReceivingId = id;
        model.ReceivingNo = await EnsureReceivingNumberAsync(model.ReceivingNo, model.ReceiveDate);
        ModelState.Remove(nameof(ReceivingFormViewModel.ReceivingNo));
        var saveDraft = IsSaveDraftCommand(command);
        if (!saveDraft && !CurrentUserHasPermission("Receiving.Post"))
        {
            return Forbid();
        }

        EnsureBranchFromPostedAllocation(model);
        await PopulateLookupsAsync(model);
        if (saveDraft ? !await ValidateReceivingDraftAsync(model) : !await ValidateReceivingAsync(model))
        {
            return View("Create", model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            receiving.ReceivingNo = model.ReceivingNo;
            receiving.ReceiveDate = model.ReceiveDate;
            receiving.SupplierId = model.SupplierId!.Value;
            receiving.PurchaseOrderId = model.PurchaseOrderId!.Value;
            receiving.BranchId = model.BranchId;
            receiving.DeliveryNoteNo = model.DeliveryNoteNo?.Trim();
            receiving.Remark = model.Remark?.Trim();
            receiving.Status = saveDraft ? "Draft" : "Posted";
            receiving.UpdatedByUserId = userId;
            receiving.PostedByUserId = saveDraft ? receiving.PostedByUserId : userId;
            receiving.PostedDate = saveDraft ? receiving.PostedDate : now;
            receiving.UpdatedDate = now;

            await ReplaceReceivingDetailsAsync(receiving, model);
            if (!saveDraft)
            {
                await ApplyPostedReceivingAsync(receiving, model);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = receiving.ReceivingId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Receiving number or serial number must be unique.");
            return View("Create", model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!CurrentUserHasPermission("Receiving.View"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var receiving = await _context.ReceivingHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrderHeader)
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.PostedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.PurchaseOrderDetail)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.ReceivingSerials)
            .FirstOrDefaultAsync(x => x.ReceivingId == id.Value);

        return receiving is null || !CanAccessBranch(receiving.BranchId) ? NotFound() : View(receiving);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(int id)
    {
        if (!CurrentUserHasPermission("Receiving.Post"))
        {
            return Forbid();
        }

        var receiving = await GetReceivingForEditAsync(id);
        if (receiving is null || !CanAccessBranch(receiving.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(receiving.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ReceivingNotice"] = "Only Draft receiving documents can be posted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = await BuildFormModelFromReceivingAsync(receiving);
        if (!await ValidateReceivingAsync(model))
        {
            TempData["ReceivingNotice"] = GetFirstModelStateErrorMessage("Post Receiving is blocked because this draft is not complete.");
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();

            receiving.Status = "Posted";
            receiving.PostedByUserId = userId;
            receiving.PostedDate = now;
            receiving.UpdatedByUserId = userId;
            receiving.UpdatedDate = now;

            await ApplyPostedReceivingAsync(receiving, model);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["ReceivingNotice"] = "Receiving posted successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            TempData["ReceivingNotice"] = "Receiving number or serial number must be unique.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (!CurrentUserHasPermission("Receiving.View"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var receiving = await _context.ReceivingHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrderHeader)
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.PostedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.PurchaseOrderDetail)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.ReceivingSerials)
            .FirstOrDefaultAsync(x => x.ReceivingId == id.Value);

        if (receiving is null || !CanAccessBranch(receiving.BranchId))
        {
            return NotFound();
        }

        if (receiving.Status != "Posted")
        {
            TempData["ReceivingNotice"] = "Print is available after receiving is posted.";
            return RedirectToAction(nameof(Details), new { id = receiving.ReceivingId });
        }

        PopulatePrintCompanyViewData(_companyProfile);
        return View(receiving);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        if (!CurrentUserHasPermission("Receiving.Cancel"))
        {
            return Forbid();
        }

        var receiving = await _context.ReceivingHeaders
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.ReceivingSerials)
            .FirstOrDefaultAsync(x => x.ReceivingId == id);

        if (receiving is null || !CanAccessBranch(receiving.BranchId))
        {
            return NotFound();
        }

        var blockReason = await GetCancelBlockedReasonAsync(receiving);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["ReceivingNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var userId = CurrentUserId();
        var reason = NormalizeCancelReason(cancelReason);

        if (receiving.Status == "Draft")
        {
            await ReleaseDraftSerialsAsync(receiving);
            receiving.Status = "Cancelled";
            receiving.CancelledByUserId = userId;
            receiving.CancelledDate = now;
            receiving.CancelReason = reason;
            receiving.UpdatedByUserId = userId;
            receiving.UpdatedDate = now;
        }
        else
        {
            await ReversePostedReceivingAsync(receiving);
            receiving.Status = "Cancelled";
            receiving.CancelledByUserId = userId;
            receiving.CancelledDate = now;
            receiving.CancelReason = reason;
            receiving.UpdatedByUserId = userId;
            receiving.UpdatedDate = now;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["ReceivingNotice"] = "Receiving cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateLookupsAsync(ReceivingFormViewModel model)
    {
        var purchaseOrders = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
                    .ThenInclude(x => x.Branch)
            .Where(x => x.Status != "Cancelled")
            .ToListAsync();

        var effectiveBranchId = CurrentUserCanAccessAllBranches() ? model.BranchId : CurrentBranchId();
        if (!CurrentUserCanAccessAllBranches())
        {
            purchaseOrders = purchaseOrders
                .Where(x => x.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == effectiveBranchId && a.AllocatedQty > a.ReceivedQty)))
                .ToList();
        }

        model.PurchaseOrderLookup = purchaseOrders
            .Select(x => new ReceivingPoLookupViewModel
            {
                PurchaseOrderId = x.PurchaseOrderId,
                PONo = x.PONo,
                SupplierId = x.SupplierId,
                SupplierName = x.Supplier?.SupplierName ?? string.Empty,
                BranchId = effectiveBranchId ?? x.BranchId,
                BranchName = effectiveBranchId.HasValue
                    ? x.PurchaseOrderDetails.SelectMany(d => d.PurchaseOrderAllocations).FirstOrDefault(a => a.BranchId == effectiveBranchId.Value)?.Branch?.BranchName ?? string.Empty
                    : x.Branch?.BranchName ?? string.Empty,
                VatType = x.VatType,
                Subtotal = x.Subtotal,
                VatAmount = x.VatAmount,
                TotalAmount = x.TotalAmount,
                Lines = x.PurchaseOrderDetails
                    .SelectMany(d => d.PurchaseOrderAllocations
                        .Where(a => (!effectiveBranchId.HasValue || a.BranchId == effectiveBranchId.Value) && a.AllocatedQty > a.ReceivedQty)
                        .Select(a => new ReceivingPoLookupLineViewModel
                        {
                            PurchaseOrderDetailId = d.PurchaseOrderDetailId,
                            PurchaseOrderAllocationId = a.PurchaseOrderAllocationId,
                            AllocationBranchId = a.BranchId,
                            AllocationBranchName = a.Branch?.BranchName ?? string.Empty,
                            ItemId = d.ItemId,
                            LineNumber = d.LineNumber,
                            ItemCode = d.Item?.ItemCode ?? string.Empty,
                            ItemName = d.Item?.ItemName ?? string.Empty,
                            IsSerialControlled = d.Item?.IsSerialControlled ?? false,
                            TrackStock = d.Item?.TrackStock ?? false,
                            OrderedQty = a.AllocatedQty,
                            ReceivedQty = a.ReceivedQty,
                            RemainingQty = a.AllocatedQty - a.ReceivedQty,
                            UnitPrice = d.UnitPrice,
                            LineTotal = d.LineTotal
                        }))
                    .OrderBy(d => d.LineNumber)
                    .ThenBy(d => d.AllocationBranchName)
                    .ToList()
            })
            .Where(x => x.Lines.Count > 0)
            .OrderByDescending(x => x.PurchaseOrderId)
            .ToList();

        model.PurchaseOrderOptions = model.PurchaseOrderLookup
            .Select(x => new SelectListItem($"{x.PONo} - {x.SupplierName} - {x.BranchName}", x.PurchaseOrderId.ToString()))
            .ToList();
    }

    private static void EnsureBranchFromPostedAllocation(ReceivingFormViewModel model)
    {
        var postedAllocationBranchIds = model.Details
            .Where(x => x.AllocationBranchId.HasValue)
            .Select(x => x.AllocationBranchId!.Value)
            .Distinct()
            .ToList();

        if (postedAllocationBranchIds.Count == 1)
        {
            model.BranchId = postedAllocationBranchIds[0];
        }
    }

    private static void PopulateDetailsFromLookup(ReceivingFormViewModel model, int purchaseOrderId)
    {
        var selected = model.PurchaseOrderLookup.FirstOrDefault(x => x.PurchaseOrderId == purchaseOrderId);
        if (selected is null)
        {
            model.Details.Clear();
            return;
        }

        model.PurchaseOrderId = selected.PurchaseOrderId;
        model.SupplierId = selected.SupplierId;
        model.BranchId = selected.BranchId;
        model.BranchName = selected.BranchName;
        model.Details = selected.Lines
            .Select(x => new ReceivingLineEditorViewModel
            {
                PurchaseOrderDetailId = x.PurchaseOrderDetailId,
                PurchaseOrderAllocationId = x.PurchaseOrderAllocationId,
                AllocationBranchId = x.AllocationBranchId,
                AllocationBranchName = x.AllocationBranchName,
                ItemId = x.ItemId,
                LineNumber = x.LineNumber,
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                IsSerialControlled = x.IsSerialControlled,
                TrackStock = x.TrackStock,
                OrderedQty = x.OrderedQty,
                ReceivedQty = x.ReceivedQty,
                RemainingQty = x.RemainingQty,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                QtyReceivedInput = 0,
                SerialEntryText = string.Empty,
                Serials = x.IsSerialControlled ? new List<ReceivingSerialEditorViewModel> { new() } : new List<ReceivingSerialEditorViewModel>()
            })
            .ToList();
    }

    private async Task<ReceivingHeader?> GetReceivingForEditAsync(int id)
    {
        return await _context.ReceivingHeaders
            .Include(x => x.Branch)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.ReceivingSerials)
            .FirstOrDefaultAsync(x => x.ReceivingId == id);
    }

    private async Task<ReceivingFormViewModel> BuildFormModelFromReceivingAsync(ReceivingHeader receiving)
    {
        var model = new ReceivingFormViewModel
        {
            ReceivingId = receiving.ReceivingId,
            ReceivingNo = receiving.ReceivingNo,
            ReceiveDate = receiving.ReceiveDate,
            PurchaseOrderId = receiving.PurchaseOrderId,
            SupplierId = receiving.SupplierId,
            BranchId = receiving.BranchId,
            BranchName = receiving.Branch?.BranchName ?? string.Empty,
            DeliveryNoteNo = receiving.DeliveryNoteNo,
            Remark = receiving.Remark,
            Status = receiving.Status
        };

        await PopulateLookupsAsync(model);
        PopulateDetailsFromLookup(model, receiving.PurchaseOrderId);

        foreach (var savedLine in receiving.ReceivingDetails)
        {
            var line = model.Details.FirstOrDefault(x =>
                x.PurchaseOrderAllocationId == savedLine.PurchaseOrderAllocationId ||
                (!savedLine.PurchaseOrderAllocationId.HasValue && x.PurchaseOrderDetailId == savedLine.PurchaseOrderDetailId));
            if (line is null)
            {
                continue;
            }

            line.QtyReceivedInput = savedLine.QtyReceived;
            line.Remark = savedLine.Remark;
            line.SupplierWarrantyStartDate = savedLine.SupplierWarrantyStartDate;
            line.SupplierWarrantyEndDate = savedLine.SupplierWarrantyEndDate;
            line.Serials = savedLine.ReceivingSerials
                .OrderBy(x => x.ReceivingSerialId)
                .Select(x => new ReceivingSerialEditorViewModel { SerialNo = x.SerialNo })
                .DefaultIfEmpty(new ReceivingSerialEditorViewModel())
                .ToList();
        }

        return model;
    }

    private async Task AddReceivingDetailsAsync(int receivingId, ReceivingFormViewModel model)
    {
        foreach (var line in NormalizeReceivingDetails(model.Details).OrderBy(x => x.LineNumber))
        {
            var receivingDetail = new ReceivingDetail
            {
                ReceivingId = receivingId,
                PurchaseOrderDetailId = line.PurchaseOrderDetailId,
                PurchaseOrderAllocationId = line.PurchaseOrderAllocationId,
                ItemId = line.ItemId,
                LineNumber = line.LineNumber,
                QtyReceived = line.QtyReceivedInput,
                Remark = line.Remark?.Trim(),
                SupplierWarrantyStartDate = line.SupplierWarrantyStartDate?.Date,
                SupplierWarrantyEndDate = line.SupplierWarrantyEndDate?.Date
            };

            _context.ReceivingDetails.Add(receivingDetail);
            await _context.SaveChangesAsync();

            foreach (var serialNo in ExtractSerialNumbers(line))
            {
                _context.ReceivingSerials.Add(new ReceivingSerial
                {
                    ReceivingDetailId = receivingDetail.ReceivingDetailId,
                    ItemId = line.ItemId,
                    SerialNo = serialNo,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ReplaceReceivingDetailsAsync(ReceivingHeader receiving, ReceivingFormViewModel model)
    {
        var existingDetails = await _context.ReceivingDetails
            .Include(x => x.ReceivingSerials)
            .Where(x => x.ReceivingId == receiving.ReceivingId)
            .ToListAsync();

        _context.ReceivingSerials.RemoveRange(existingDetails.SelectMany(x => x.ReceivingSerials));
        _context.ReceivingDetails.RemoveRange(existingDetails);
        await _context.SaveChangesAsync();

        await AddReceivingDetailsAsync(receiving.ReceivingId, model);
    }

    private async Task ApplyPostedReceivingAsync(ReceivingHeader receiving, ReceivingFormViewModel model)
    {
        var po = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .FirstAsync(x => x.PurchaseOrderId == receiving.PurchaseOrderId);

        var details = await _context.ReceivingDetails
            .Include(x => x.Item)
            .Include(x => x.ReceivingSerials)
            .Where(x => x.ReceivingId == receiving.ReceivingId)
            .ToListAsync();

        foreach (var detail in details.OrderBy(x => x.LineNumber))
        {
            var poDetail = po.PurchaseOrderDetails.First(x => x.PurchaseOrderDetailId == detail.PurchaseOrderDetailId);
            poDetail.ReceivedQty += detail.QtyReceived;
            var allocation = detail.PurchaseOrderAllocationId.HasValue
                ? poDetail.PurchaseOrderAllocations.FirstOrDefault(x => x.PurchaseOrderAllocationId == detail.PurchaseOrderAllocationId.Value)
                : null;
            if (allocation is not null)
            {
                allocation.ReceivedQty += detail.QtyReceived;
            }

            if (detail.Item?.TrackStock == true)
            {
                detail.Item.CurrentStock += detail.QtyReceived;
                await AdjustStockBalanceAsync(receiving.BranchId, detail.ItemId, detail.QtyReceived);
                _context.StockMovements.Add(new StockMovement
                {
                    MovementDate = receiving.ReceiveDate,
                    MovementType = "Receiving",
                    ReferenceType = "Receiving",
                    ReferenceId = receiving.ReceivingId,
                    ItemId = detail.ItemId,
                    ToBranchId = receiving.BranchId,
                    Qty = detail.QtyReceived,
                    Remark = receiving.ReceivingNo,
                    CreatedByUserId = CurrentUserId(),
                    CreatedDate = DateTime.UtcNow
                });
            }

            if (detail.Item?.IsSerialControlled == true)
            {
                foreach (var serial in detail.ReceivingSerials)
                {
                    _context.SerialNumbers.Add(new SerialNumber
                    {
                        ItemId = detail.ItemId,
                        SerialNo = serial.SerialNo,
                        Status = "InStock",
                        SupplierId = receiving.SupplierId,
                        BranchId = receiving.BranchId,
                        CurrentCustomerId = null,
                        InvoiceId = null,
                        SupplierWarrantyStartDate = detail.SupplierWarrantyStartDate?.Date,
                        SupplierWarrantyEndDate = detail.SupplierWarrantyEndDate?.Date,
                        CustomerWarrantyStartDate = null,
                        CustomerWarrantyEndDate = null,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }
        }

        po.Status = ComputePOStatus(po);
        po.UpdatedByUserId = CurrentUserId();
        po.UpdatedDate = DateTime.UtcNow;
    }

    private async Task<string?> GetCancelBlockedReasonAsync(ReceivingHeader receiving)
    {
        if (receiving.Status == "Cancelled")
        {
            return "Cancelled receiving documents are read-only.";
        }

        if (receiving.Status == "Draft")
        {
            return null;
        }

        if (receiving.Status != "Posted")
        {
            return $"Cancel Receiving is available only for Draft or Posted receiving documents. Current status is {receiving.Status}.";
        }

        foreach (var detail in receiving.ReceivingDetails)
        {
            if (detail.Item?.TrackStock == true && detail.Item.CurrentStock < detail.QtyReceived)
            {
                return $"Cancel Receiving is blocked because item {detail.Item.ItemCode} does not have enough stock to reverse.";
            }
        }

        var serialNos = receiving.ReceivingDetails
            .SelectMany(x => x.ReceivingSerials)
            .Select(x => x.SerialNo)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (serialNos.Count == 0)
        {
            return null;
        }

        var serials = await _context.SerialNumbers
            .AsNoTracking()
            .Where(x => serialNos.Contains(x.SerialNo))
            .ToListAsync();

        if (serials.Count != serialNos.Count)
        {
            return "Cancel Receiving is blocked because one or more receiving serials are no longer in stock records.";
        }

        var unavailable = serials.FirstOrDefault(x =>
            !string.Equals(x.Status, "InStock", StringComparison.OrdinalIgnoreCase) ||
            x.InvoiceId.HasValue ||
            x.CurrentCustomerId.HasValue);
        if (unavailable is not null)
        {
            return $"Cancel Receiving is blocked because serial {unavailable.SerialNo} has already been used by another workflow.";
        }

        var serialIds = serials.Select(x => x.SerialId).ToList();
        var hasClaim = await _context.SerialClaimLogs
            .AsNoTracking()
            .AnyAsync(x => serialIds.Contains(x.SerialId));
        if (hasClaim)
        {
            return "Cancel Receiving is blocked because one or more serials already have supplier claim history.";
        }

        return null;
    }

    private async Task ReleaseDraftSerialsAsync(ReceivingHeader receiving)
    {
        var serials = receiving.ReceivingDetails.SelectMany(x => x.ReceivingSerials).ToList();
        if (serials.Count > 0)
        {
            _context.ReceivingSerials.RemoveRange(serials);
            await _context.SaveChangesAsync();
        }
    }

    private async Task ReversePostedReceivingAsync(ReceivingHeader receiving)
    {
        var po = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .FirstAsync(x => x.PurchaseOrderId == receiving.PurchaseOrderId);

        var serialNos = receiving.ReceivingDetails
            .SelectMany(x => x.ReceivingSerials)
            .Select(x => x.SerialNo)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var serialNumbers = serialNos.Count == 0
            ? new List<SerialNumber>()
            : await _context.SerialNumbers
                .Where(x => serialNos.Contains(x.SerialNo))
                .ToListAsync();

        foreach (var detail in receiving.ReceivingDetails)
        {
            var poDetail = po.PurchaseOrderDetails.First(x => x.PurchaseOrderDetailId == detail.PurchaseOrderDetailId);
            poDetail.ReceivedQty = Math.Max(0m, poDetail.ReceivedQty - detail.QtyReceived);
            var allocation = detail.PurchaseOrderAllocationId.HasValue
                ? poDetail.PurchaseOrderAllocations.FirstOrDefault(x => x.PurchaseOrderAllocationId == detail.PurchaseOrderAllocationId.Value)
                : null;
            if (allocation is not null)
            {
                allocation.ReceivedQty = Math.Max(0m, allocation.ReceivedQty - detail.QtyReceived);
            }

            if (detail.Item?.TrackStock == true)
            {
                detail.Item.CurrentStock -= detail.QtyReceived;
                await AdjustStockBalanceAsync(receiving.BranchId, detail.ItemId, -detail.QtyReceived);
                _context.StockMovements.Add(new StockMovement
                {
                    MovementDate = DateTime.Today,
                    MovementType = "ReceivingCancel",
                    ReferenceType = "Receiving",
                    ReferenceId = receiving.ReceivingId,
                    ItemId = detail.ItemId,
                    FromBranchId = receiving.BranchId,
                    Qty = -detail.QtyReceived,
                    Remark = receiving.ReceivingNo,
                    CreatedByUserId = CurrentUserId(),
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        if (serialNumbers.Count > 0)
        {
            _context.SerialNumbers.RemoveRange(serialNumbers);
        }

        po.Status = ComputePOStatus(po);
        po.UpdatedByUserId = CurrentUserId();
        po.UpdatedDate = DateTime.UtcNow;
    }

    private async Task<bool> ValidateReceivingDraftAsync(ReceivingFormViewModel model)
    {
        if (!model.PurchaseOrderId.HasValue)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Please select a purchase order.");
            return false;
        }

        var selectedLookup = model.PurchaseOrderLookup.FirstOrDefault(x => x.PurchaseOrderId == model.PurchaseOrderId.Value);
        if (selectedLookup is null)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Selected PO is not available for receiving.");
            return false;
        }

        model.SupplierId = selectedLookup.SupplierId;
        model.BranchId = selectedLookup.BranchId;
        model.BranchName = selectedLookup.BranchName;
        var supplierExists = await _context.Suppliers.AnyAsync(x => x.SupplierId == model.SupplierId.Value);
        if (!supplierExists)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Selected supplier was not found.");
        }

        var requestLines = model.Details
            .Select((line, index) => new { Line = line, Index = index })
            .Where(x => x.Line.QtyReceivedInput > 0)
            .ToList();

        foreach (var requestLine in requestLines)
        {
            var line = requestLine.Line;
            var i = requestLine.Index;
            var lookupLine = selectedLookup.Lines.FirstOrDefault(x =>
                x.PurchaseOrderAllocationId == line.PurchaseOrderAllocationId ||
                (!line.PurchaseOrderAllocationId.HasValue && x.PurchaseOrderDetailId == line.PurchaseOrderDetailId));
            if (lookupLine is null)
            {
                ModelState.AddModelError(nameof(model.Details), "One or more receiving lines are not valid for the selected PO.");
                continue;
            }

            if (line.QtyReceivedInput > lookupLine.RemainingQty)
            {
                ModelState.AddModelError($"Details[{i}].QtyReceivedInput", "Qty received cannot exceed remaining PO quantity.");
            }

            if (lookupLine.IsSerialControlled && line.QtyReceivedInput != Math.Truncate(line.QtyReceivedInput))
            {
                ModelState.AddModelError($"Details[{i}].QtyReceivedInput", "Serial-controlled items must be received in whole numbers.");
            }

            var serials = ExtractSerialNumbers(line);
            if (serials.Count != serials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                ModelState.AddModelError($"Details[{i}].SerialEntryText", "Duplicate serial numbers are not allowed in the same receiving line.");
            }

            if (line.SupplierWarrantyStartDate.HasValue &&
                line.SupplierWarrantyEndDate.HasValue &&
                line.SupplierWarrantyEndDate.Value.Date < line.SupplierWarrantyStartDate.Value.Date)
            {
                ModelState.AddModelError($"Details[{i}].SupplierWarrantyEndDate", "Supplier warranty end date must be on or after the supplier warranty start date.");
            }
        }

        var serialNos = requestLines
            .SelectMany(x => ExtractSerialNumbers(x.Line))
            .ToList();

        if (serialNos.Count != serialNos.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            ModelState.AddModelError(string.Empty, "Duplicate serial numbers are not allowed.");
        }

        if (serialNos.Count > 0)
        {
            var existingSerials = await _context.SerialNumbers
                .AsNoTracking()
                .AnyAsync(x => serialNos.Contains(x.SerialNo));

            if (existingSerials)
            {
                ModelState.AddModelError(string.Empty, "One or more serial numbers already exist in stock.");
            }
        }

        return ModelState.IsValid;
    }

    private async Task<bool> ValidateReceivingAsync(ReceivingFormViewModel model)
    {
        if (!model.PurchaseOrderId.HasValue)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Please select a purchase order.");
            return false;
        }

        var postedLines = NormalizeReceivingDetails(model.Details);
        if (postedLines.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "Enter quantity for at least one receiving line.");
            return false;
        }

        var selectedLookup = model.PurchaseOrderLookup.FirstOrDefault(x => x.PurchaseOrderId == model.PurchaseOrderId.Value);
        if (selectedLookup is null)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Selected PO is not available for receiving.");
            return false;
        }

        model.SupplierId = selectedLookup.SupplierId;
        model.BranchId = selectedLookup.BranchId;
        model.BranchName = selectedLookup.BranchName;
        var supplierExists = await _context.Suppliers.AnyAsync(x => x.SupplierId == model.SupplierId.Value);
        if (!supplierExists)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Selected supplier was not found.");
        }

        var requestLines = model.Details
            .Select((line, index) => new { Line = line, Index = index })
            .Where(x => x.Line.QtyReceivedInput > 0)
            .ToList();

        foreach (var requestLine in requestLines)
        {
            var line = requestLine.Line;
            var i = requestLine.Index;
            var lookupLine = selectedLookup.Lines.FirstOrDefault(x =>
                x.PurchaseOrderAllocationId == line.PurchaseOrderAllocationId ||
                (!line.PurchaseOrderAllocationId.HasValue && x.PurchaseOrderDetailId == line.PurchaseOrderDetailId));
            if (lookupLine is null)
            {
                ModelState.AddModelError(nameof(model.Details), "One or more receiving lines are not valid for the selected PO.");
                continue;
            }

            if (line.QtyReceivedInput > lookupLine.RemainingQty)
            {
                ModelState.AddModelError($"Details[{i}].QtyReceivedInput", "Qty received cannot exceed remaining PO quantity.");
            }

            if (lookupLine.IsSerialControlled)
            {
                var serials = ExtractSerialNumbers(line);

                if (line.QtyReceivedInput != Math.Truncate(line.QtyReceivedInput))
                {
                    ModelState.AddModelError($"Details[{i}].QtyReceivedInput", "Serial-controlled items must be received in whole numbers.");
                }

                if (serials.Count == 0)
                {
                    ModelState.AddModelError($"Details[{i}].SerialEntryText", "Serial numbers are required for serial-controlled items.");
                }

                if (serials.Count != (int)line.QtyReceivedInput)
                {
                    ModelState.AddModelError($"Details[{i}].SerialEntryText", "Serial count must exactly match qty received for serial-controlled items.");
                }

                if (serials.Count != serials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                {
                    ModelState.AddModelError($"Details[{i}].SerialEntryText", "Duplicate serial numbers are not allowed in the same receiving line.");
                }

                if (line.SupplierWarrantyStartDate.HasValue &&
                    line.SupplierWarrantyEndDate.HasValue &&
                    line.SupplierWarrantyEndDate.Value.Date < line.SupplierWarrantyStartDate.Value.Date)
                {
                    ModelState.AddModelError($"Details[{i}].SupplierWarrantyEndDate", "Supplier warranty end date must be on or after the supplier warranty start date.");
                }
            }
        }

        var serialNos = requestLines
            .SelectMany(x => ExtractSerialNumbers(x.Line))
            .ToList();

        if (serialNos.Count != serialNos.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            ModelState.AddModelError(string.Empty, "Duplicate serial numbers are not allowed.");
        }

        if (serialNos.Count > 0)
        {
            var existingSerials = await _context.SerialNumbers
                .AsNoTracking()
                .AnyAsync(x => serialNos.Contains(x.SerialNo));

            if (existingSerials)
            {
                ModelState.AddModelError(string.Empty, "One or more serial numbers already exist in stock.");
            }
        }

        return ModelState.IsValid;
    }

    private static List<string> ExtractSerialNumbers(ReceivingLineEditorViewModel line)
    {
        if (!string.IsNullOrWhiteSpace(line.SerialEntryText))
        {
            return line.SerialEntryText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        return (line.Serials ?? new List<ReceivingSerialEditorViewModel>())
            .Select(x => x.SerialNo?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
    }

    private static List<ReceivingLineEditorViewModel> NormalizeReceivingDetails(IEnumerable<ReceivingLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<ReceivingLineEditorViewModel>())
            .Where(x => x.QtyReceivedInput > 0)
            .ToList();
    }

    private string GetFirstModelStateErrorMessage(string fallback)
    {
        return ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? fallback;
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

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private static bool IsSaveDraftCommand(string? command)
    {
        return string.Equals(command, "SaveDraft", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeCancelReason(string? cancelReason)
    {
        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            return null;
        }

        var trimmed = cancelReason.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    private static string ComputePOStatus(PurchaseOrderHeader po)
    {
        if (po.Status == "Cancelled")
        {
            return po.Status;
        }

        var allReceived = po.PurchaseOrderDetails.All(x => x.ReceivedQty >= x.Qty);
        if (allReceived)
        {
            return "FullyReceived";
        }

        var anyReceived = po.PurchaseOrderDetails.Any(x => x.ReceivedQty > 0);
        return anyReceived ? "PartiallyReceived" : "Approved";
    }

    private Task<string> GetNextReceivingNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.ReceivingHeaders.Select(x => x.ReceivingNo), prefix, date);
    }

    private async Task<string> EnsureReceivingNumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextReceivingNumberAsync(date)
            : existingNo.Trim();
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
