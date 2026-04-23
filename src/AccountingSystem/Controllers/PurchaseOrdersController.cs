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
public class PurchaseOrdersController : CrudControllerBase
{
    private const string NumberPrefix = "PO";
    private readonly AccountingDbContext _context;
    private readonly PurchaseWorkflowEmailService _purchaseWorkflowEmailService;
    private readonly CompanyProfileSettings _companyProfile;

    public PurchaseOrdersController(
        AccountingDbContext context,
        PurchaseWorkflowEmailService purchaseWorkflowEmailService,
        IOptions<CompanyProfileSettings> companyProfileOptions)
    {
        _context = context;
        _purchaseWorkflowEmailService = purchaseWorkflowEmailService;
        _companyProfile = companyProfileOptions.Value;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        if (!CurrentUserHasPermission("PO.View"))
        {
            return Forbid();
        }

        var query = _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.PurchaseRequestHeader)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .AsQueryable();

        if (!CurrentUserCanAccessAllBranches())
        {
            var branchId = CurrentBranchId();
            query = query.Where(x => x.BranchId == branchId ||
                x.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == branchId)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.PONo.Contains(keyword) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(keyword)) ||
                (x.PurchaseRequestHeader != null && x.PurchaseRequestHeader.PRNo.Contains(keyword)) ||
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
            query = query.Where(x => x.PODate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.PODate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var orders = await PaginatedList<PurchaseOrderHeader>.CreateAsync(query
            .OrderByDescending(x => x.PODate)
            .ThenByDescending(x => x.PurchaseOrderId), page, pageSize);

        return View(orders);
    }

    public async Task<IActionResult> Create(int? purchaseRequestId, int[]? purchaseRequestIds)
    {
        if (!CurrentUserHasPermission("PO.Create"))
        {
            return Forbid();
        }

        var model = new PurchaseOrderFormViewModel
        {
            PONo = await GetNextPONumberAsync(DateTime.Today),
            BranchId = CurrentBranchId()
        };

        var selectedRequestIds = (purchaseRequestIds ?? Array.Empty<int>())
            .Concat(purchaseRequestId.HasValue ? new[] { purchaseRequestId.Value } : Array.Empty<int>())
            .Distinct()
            .ToList();

        if (selectedRequestIds.Count > 0)
        {
            var requests = await _context.PurchaseRequestHeaders
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.PurchaseRequestDetails)
                    .ThenInclude(x => x.Item)
                .Include(x => x.PurchaseRequestDetails)
                    .ThenInclude(x => x.PurchaseOrderAllocationSources)
                .Where(x => selectedRequestIds.Contains(x.PurchaseRequestId))
                .ToListAsync();

            if (requests.Count != selectedRequestIds.Count || requests.Any(x => !CanAccessBranch(x.BranchId)))
            {
                return NotFound();
            }

            if (requests.Any(x => x.Status != "Approved" || x.PurchaseRequestDetails.Any(d => d.PurchaseOrderAllocationSources.Any())))
            {
                TempData["PurchaseOrderNotice"] = "Create PO from PR is available only for Approved purchase requests that have not been converted to PO.";
                return RedirectToAction("Index", "PurchaseRequests");
            }

            ApplyPurchaseRequestSources(model, requests);
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderFormViewModel model)
    {
        if (!CurrentUserHasPermission("PO.Create"))
        {
            return Forbid();
        }

        model.PONo = await EnsurePONumberAsync(model.PONo, model.PODate);
        model.Status = "Draft";
        ModelState.Remove(nameof(PurchaseOrderFormViewModel.PONo));
        ModelState.Remove(nameof(PurchaseOrderFormViewModel.Status));

        if (!await ValidateAndComputeAsync(model, null))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var header = new PurchaseOrderHeader
        {
            PONo = model.PONo,
            PODate = model.PODate,
            SupplierId = model.SupplierId!.Value,
            BranchId = model.BranchId,
            PurchaseRequestId = model.PurchaseRequestId,
            ReferenceNo = model.ReferenceNo?.Trim(),
            ExpectedReceiveDate = model.ExpectedReceiveDate,
            Remark = model.Remark?.Trim(),
            Subtotal = model.Subtotal,
            DiscountAmount = model.DiscountAmount,
            VatType = model.VatType,
            VatAmount = model.VatAmount,
            TotalAmount = model.TotalAmount,
            Status = "Draft",
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow,
            PurchaseOrderDetails = model.Details.Select(MapDetailEntity).ToList()
        };

        _context.PurchaseOrderHeaders.Add(header);
        var linkedRequests = await GetValidPurchaseRequestsForPoAsync(model);
        foreach (var linkedRequest in linkedRequests)
        {
            linkedRequest.Status = "ConvertedToPO";
            linkedRequest.UpdatedByUserId = CurrentUserId();
            linkedRequest.UpdatedDate = DateTime.UtcNow;
        }

        if (!await TrySaveAsync("PO number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = header.PurchaseOrderId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (!CurrentUserHasPermission("PO.Edit"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var header = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.PurchaseRequestHeader)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
                    .ThenInclude(x => x.PurchaseOrderAllocationSources)
                        .ThenInclude(x => x.PurchaseRequestDetail!)
                            .ThenInclude(x => x.PurchaseRequestHeader)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id.Value);

        if (header is null || !CanAccessBranch(header.BranchId))
        {
            return NotFound();
        }

        if (!CanEditOrder(header))
        {
            TempData["PurchaseOrderNotice"] = $"Only Draft or Rejected purchase orders can be edited. Current status is {header.Status}.";
            return RedirectToAction(nameof(Details), new { id = header.PurchaseOrderId });
        }

        var model = new PurchaseOrderFormViewModel
        {
            PurchaseOrderId = header.PurchaseOrderId,
            PONo = header.PONo,
            PODate = header.PODate,
            SupplierId = header.SupplierId,
            BranchId = header.BranchId,
            PurchaseRequestId = header.PurchaseRequestId,
            PurchaseRequestNo = header.PurchaseRequestHeader?.PRNo,
            ReferenceNo = header.ReferenceNo,
            ExpectedReceiveDate = header.ExpectedReceiveDate,
            Remark = header.Remark,
            Subtotal = header.Subtotal,
            DiscountAmount = header.DiscountAmount,
            VatType = header.VatType,
            VatAmount = header.VatAmount,
            TotalAmount = header.TotalAmount,
            Status = header.Status,
            Details = header.PurchaseOrderDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new PurchaseOrderLineEditorViewModel
                {
                    PurchaseOrderDetailId = x.PurchaseOrderDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    Qty = x.Qty,
                    ReceivedQty = x.ReceivedQty,
                    UnitPrice = x.UnitPrice,
                    DiscountAmount = x.DiscountAmount,
                    LineTotal = x.LineTotal,
                    Remark = x.Remark,
                    Allocations = x.PurchaseOrderAllocations
                        .OrderBy(a => a.Branch != null ? a.Branch.BranchCode : string.Empty)
                        .Select(a => new PurchaseOrderAllocationEditorViewModel
                        {
                            PurchaseOrderAllocationId = a.PurchaseOrderAllocationId,
                            BranchId = a.BranchId,
                            AllocatedQty = a.AllocatedQty,
                            ReceivedQty = a.ReceivedQty,
                            Sources = a.PurchaseOrderAllocationSources
                                .OrderBy(s => s.PurchaseRequestDetail?.PurchaseRequestHeader?.PRNo)
                                .Select(s => new PurchaseOrderAllocationSourceEditorViewModel
                                {
                                    PurchaseOrderAllocationSourceId = s.PurchaseOrderAllocationSourceId,
                                    PurchaseRequestDetailId = s.PurchaseRequestDetailId,
                                    PurchaseRequestId = s.PurchaseRequestDetail?.PurchaseRequestId,
                                    PurchaseRequestNo = s.PurchaseRequestDetail?.PurchaseRequestHeader?.PRNo ?? string.Empty,
                                    SourceQty = s.SourceQty
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };

        if (model.Details.Count == 0)
        {
            model.Details.Add(new PurchaseOrderLineEditorViewModel { LineNumber = 1 });
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseOrderFormViewModel model)
    {
        if (!CurrentUserHasPermission("PO.Edit"))
        {
            return Forbid();
        }

        if (id != model.PurchaseOrderId)
        {
            return NotFound();
        }

        var existingHeader = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
                    .ThenInclude(x => x.PurchaseOrderAllocationSources)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (existingHeader is null || !CanAccessBranch(existingHeader.BranchId))
        {
            return NotFound();
        }

        if (!CanEditOrder(existingHeader))
        {
            TempData["PurchaseOrderNotice"] = $"Only Draft or Rejected purchase orders can be edited. Current status is {existingHeader.Status}.";
            return RedirectToAction(nameof(Details), new { id = existingHeader.PurchaseOrderId });
        }

        if (existingHeader.PurchaseOrderDetails.Any(x => x.ReceivedQty > 0))
        {
            ModelState.AddModelError(string.Empty, "PO lines cannot be edited after receiving has been posted. Create a new PO or continue with receiving only.");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        model.Status = existingHeader.Status;
        ModelState.Remove(nameof(PurchaseOrderFormViewModel.Status));

        if (!await ValidateAndComputeAsync(model, existingHeader))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        existingHeader.PONo = model.PONo.Trim();
        existingHeader.PODate = model.PODate;
        existingHeader.SupplierId = model.SupplierId!.Value;
        existingHeader.BranchId = model.BranchId;
        existingHeader.PurchaseRequestId = model.PurchaseRequestId;
        existingHeader.ReferenceNo = model.ReferenceNo?.Trim();
        existingHeader.ExpectedReceiveDate = model.ExpectedReceiveDate;
        existingHeader.Remark = model.Remark?.Trim();
        existingHeader.Subtotal = model.Subtotal;
        existingHeader.DiscountAmount = model.DiscountAmount;
        existingHeader.VatType = model.VatType;
        existingHeader.VatAmount = model.VatAmount;
        existingHeader.TotalAmount = model.TotalAmount;
        if (existingHeader.Status == "Rejected")
        {
            existingHeader.Status = "Draft";
            existingHeader.RejectedByUserId = null;
            existingHeader.RejectedDate = null;
            existingHeader.RejectReason = null;
        }

        existingHeader.UpdatedByUserId = CurrentUserId();
        existingHeader.UpdatedDate = DateTime.UtcNow;

        _context.PurchaseOrderDetails.RemoveRange(existingHeader.PurchaseOrderDetails);
        existingHeader.PurchaseOrderDetails = model.Details.Select(MapDetailEntity).ToList();

        if (!await TrySaveAsync("PO number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = existingHeader.PurchaseOrderId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!CurrentUserHasPermission("PO.View"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.PurchaseRequestHeader)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.RejectedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
                    .ThenInclude(x => x.Branch)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
                    .ThenInclude(x => x.PurchaseOrderAllocationSources)
                        .ThenInclude(x => x.PurchaseRequestDetail!)
                            .ThenInclude(x => x.PurchaseRequestHeader)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id.Value);

        return order is null || !CanAccessOrder(order) ? NotFound() : View(order);
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (!CurrentUserHasPermission("PO.View"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.PurchaseRequestHeader)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.RejectedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
                    .ThenInclude(x => x.Branch)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
                    .ThenInclude(x => x.PurchaseOrderAllocationSources)
                        .ThenInclude(x => x.PurchaseRequestDetail!)
                            .ThenInclude(x => x.PurchaseRequestHeader)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id.Value);

        if (order is null || !CanAccessOrder(order))
        {
            return NotFound();
        }

        PopulatePrintCompanyViewData(_companyProfile);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        if (!CurrentUserHasPermission("PO.Approve"))
        {
            return Forbid();
        }

        var order = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (order is null || !CanAccessBranch(order.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetApproveBlockedReason(order);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseOrderNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        order.Status = "Approved";
        order.ApprovedByUserId = CurrentUserId();
        order.ApprovedDate = now;
        order.RejectedByUserId = null;
        order.RejectedDate = null;
        order.RejectReason = null;
        order.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseOrderNotice"] = "Purchase order approved successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        if (!CurrentUserHasPermission("PO.Submit"))
        {
            return Forbid();
        }

        var order = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (order is null || !CanAccessBranch(order.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetSubmitBlockedReason(order);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseOrderNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        order.Status = "Submitted";
        order.RejectedByUserId = null;
        order.RejectedDate = null;
        order.RejectReason = null;
        order.UpdatedDate = now;
        await _context.SaveChangesAsync();
        await _purchaseWorkflowEmailService.NotifyPoSubmittedAsync(order.PurchaseOrderId);

        TempData["PurchaseOrderNotice"] = "Purchase order submitted for approval.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? rejectReason)
    {
        if (!CurrentUserHasPermission("PO.Reject"))
        {
            return Forbid();
        }

        var order = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (order is null || !CanAccessBranch(order.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetRejectBlockedReason(order, rejectReason);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseOrderNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        order.Status = "Rejected";
        order.RejectedByUserId = CurrentUserId();
        order.RejectedDate = now;
        order.RejectReason = NormalizeReason(rejectReason);
        order.ApprovedByUserId = null;
        order.ApprovedDate = null;
        order.UpdatedDate = now;
        await _context.SaveChangesAsync();
        await _purchaseWorkflowEmailService.NotifyPoRejectedAsync(order.PurchaseOrderId);

        TempData["PurchaseOrderNotice"] = "Purchase order rejected and returned for correction.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        if (!CurrentUserHasPermission("PO.Cancel"))
        {
            return Forbid();
        }

        var order = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (order is null || !CanAccessBranch(order.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetCancelBlockedReason(order);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseOrderNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        order.Status = "Cancelled";
        order.CancelledByUserId = CurrentUserId();
        order.CancelledDate = now;
        order.CancelReason = NormalizeCancelReason(cancelReason);
        order.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseOrderNotice"] = "Purchase order cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateLookupsAsync(PurchaseOrderFormViewModel model)
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierCode)
            .ToListAsync();

        model.SupplierOptions = suppliers
            .Select(x => new SelectListItem($"{x.SupplierCode} - {x.SupplierName}", x.SupplierId.ToString()))
            .ToList();

        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        model.CanAccessAllBranches = canAccessAllBranches;
        if (!canAccessAllBranches)
        {
            model.BranchId = CurrentBranchId();
        }

        var branches = await _context.Branches
            .AsNoTracking()
            .Where(x => x.IsActive || x.BranchId == model.BranchId)
            .OrderBy(x => x.BranchCode)
            .ToListAsync();

        model.BranchOptions = branches
            .Select(x => new SelectListItem($"{x.BranchCode} - {x.BranchName}", x.BranchId.ToString(), x.BranchId == model.BranchId))
            .ToList();

        model.BranchName = branches.FirstOrDefault(x => x.BranchId == model.BranchId)?.BranchName ?? "No Branch";

        if (model.PurchaseRequestId.HasValue && string.IsNullOrWhiteSpace(model.PurchaseRequestNo))
        {
            model.PurchaseRequestNo = await _context.PurchaseRequestHeaders
                .AsNoTracking()
                .Where(x => x.PurchaseRequestId == model.PurchaseRequestId.Value)
                .Select(x => x.PRNo)
                .FirstOrDefaultAsync();
        }

        model.SupplierLookup = suppliers
            .Select(x => new PurchaseOrderSupplierLookupViewModel
            {
                SupplierId = x.SupplierId,
                SupplierCode = x.SupplierCode,
                SupplierName = x.SupplierName,
                TaxId = x.TaxId ?? string.Empty,
                Phone = x.PhoneNumber ?? string.Empty,
                Email = x.Email ?? string.Empty,
                Address = x.Address ?? string.Empty
            })
            .ToList();

        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .OrderBy(x => x.ItemCode)
            .Select(x => new QuotationItemLookupViewModel
            {
                ItemId = x.ItemId,
                DisplayText = $"{x.ItemCode} - {x.ItemName}",
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                PartNumber = x.PartNumber,
                ItemType = x.ItemType,
                UnitPrice = x.UnitPrice,
                CurrentStock = x.CurrentStock,
                TrackStock = x.TrackStock,
                IsSerialControlled = x.IsSerialControlled
            })
            .ToListAsync();

        model.StatusOptions = new[]
        {
            new SelectListItem("Draft", "Draft"),
            new SelectListItem("Submitted", "Submitted"),
            new SelectListItem("Approved", "Approved"),
            new SelectListItem("Rejected", "Rejected"),
            new SelectListItem("Cancelled", "Cancelled")
        };

        model.VatTypeOptions = new[]
        {
            new SelectListItem("VAT", "VAT"),
            new SelectListItem("No VAT", "NoVAT")
        };
    }

    private void ApplyPurchaseRequestSources(PurchaseOrderFormViewModel model, List<PurchaseRequestHeader> requests)
    {
        var orderedRequests = requests.OrderBy(x => x.PRNo).ToList();
        model.PurchaseRequestIds = orderedRequests.Select(x => x.PurchaseRequestId).ToList();
        model.PurchaseRequestId = orderedRequests.Count == 1 ? orderedRequests[0].PurchaseRequestId : null;
        model.PurchaseRequestNo = string.Join(", ", orderedRequests.Select(x => x.PRNo));
        model.PurchaseRequestSourceSummary = $"{orderedRequests.Count} PR source(s): {model.PurchaseRequestNo}";
        model.ReferenceNo = model.PurchaseRequestNo;
        var sourceBranchIds = orderedRequests
            .Where(x => x.BranchId.HasValue)
            .Select(x => x.BranchId!.Value)
            .Distinct()
            .ToList();
        if (sourceBranchIds.Count == 1)
        {
            model.BranchId = sourceBranchIds[0];
        }

        model.ExpectedReceiveDate = orderedRequests
            .Where(x => x.RequiredDate.HasValue)
            .Select(x => x.RequiredDate)
            .OrderBy(x => x)
            .FirstOrDefault();
        model.Remark = string.Join(Environment.NewLine, orderedRequests
            .Where(x => !string.IsNullOrWhiteSpace(x.Purpose))
            .Select(x => $"{x.PRNo}: {x.Purpose!.Trim()}"));

        model.Details = orderedRequests
            .SelectMany(request => request.PurchaseRequestDetails.Select(detail => new { Request = request, Detail = detail }))
            .GroupBy(x => new { x.Detail.ItemId, UnitPrice = x.Detail.Item?.UnitPrice ?? 0m })
            .OrderBy(g => g.Min(x => x.Detail.LineNumber))
            .Select((group, index) =>
            {
                var qty = group.Sum(x => x.Detail.RequestedQty);
                return new PurchaseOrderLineEditorViewModel
                {
                    LineNumber = index + 1,
                    ItemId = group.Key.ItemId,
                    Qty = qty,
                    UnitPrice = group.Key.UnitPrice,
                    DiscountAmount = 0m,
                    LineTotal = qty * group.Key.UnitPrice,
                    Remark = string.Join("; ", group
                        .Where(x => !string.IsNullOrWhiteSpace(x.Detail.Remark))
                        .Select(x => $"{x.Request.PRNo}: {x.Detail.Remark!.Trim()}")),
                    Allocations = group
                        .Where(x => x.Request.BranchId.HasValue)
                        .GroupBy(x => x.Request.BranchId!.Value)
                        .Select(branchGroup => new PurchaseOrderAllocationEditorViewModel
                        {
                            BranchId = branchGroup.Key,
                            BranchName = branchGroup.First().Request.Branch?.BranchName ?? string.Empty,
                            AllocatedQty = branchGroup.Sum(x => x.Detail.RequestedQty),
                            Sources = branchGroup
                                .OrderBy(x => x.Request.PRNo)
                                .ThenBy(x => x.Detail.LineNumber)
                                .Select(x => new PurchaseOrderAllocationSourceEditorViewModel
                                {
                                    PurchaseRequestDetailId = x.Detail.PurchaseRequestDetailId,
                                    PurchaseRequestId = x.Request.PurchaseRequestId,
                                    PurchaseRequestNo = x.Request.PRNo,
                                    SourceQty = x.Detail.RequestedQty
                                })
                                .ToList()
                        })
                        .ToList()
                };
            })
            .ToList();
    }

    private async Task<bool> ValidateAndComputeAsync(PurchaseOrderFormViewModel model, PurchaseOrderHeader? existingHeader)
    {
        model.Details = NormalizeDetails(model.Details);

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "Please add at least one PO line.");
        }

        if (!ModelState.IsValid)
        {
            if (model.Details.Count == 0)
            {
                model.Details.Add(new PurchaseOrderLineEditorViewModel { LineNumber = 1 });
            }

            return false;
        }

        var supplierExists = await _context.Suppliers.AnyAsync(x => x.SupplierId == model.SupplierId);
        if (!supplierExists)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Selected supplier was not found.");
        }

        if (!model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Please select a branch.");
        }
        else if (!CanAccessBranch(model.BranchId))
        {
            ModelState.AddModelError(nameof(model.BranchId), "You cannot create or edit purchase orders for this branch.");
        }
        else if (!await _context.Branches.AnyAsync(x => x.BranchId == model.BranchId.Value && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.BranchId), "Selected branch was not found or inactive.");
        }

        model.PurchaseRequestIds = model.Details
            .SelectMany(x => x.Allocations)
            .SelectMany(x => x.Sources)
            .Where(x => x.PurchaseRequestId.HasValue)
            .Select(x => x.PurchaseRequestId!.Value)
            .Distinct()
            .ToList();

        if (model.PurchaseRequestId.HasValue)
        {
            var request = await _context.PurchaseRequestHeaders
                .AsNoTracking()
                .Include(x => x.PurchaseOrderHeaders)
                .FirstOrDefaultAsync(x => x.PurchaseRequestId == model.PurchaseRequestId.Value);

            if (request is null)
            {
                ModelState.AddModelError(nameof(model.PurchaseRequestId), "Selected purchase request was not found.");
            }
            else if (!CanAccessBranch(request.BranchId))
            {
                ModelState.AddModelError(nameof(model.PurchaseRequestId), "You cannot create a PO from this purchase request branch.");
            }
            else if (existingHeader is null && request.Status != "Approved")
            {
                ModelState.AddModelError(nameof(model.PurchaseRequestId), "PO can be created from Approved purchase requests only.");
            }
            else if (existingHeader is null && request.PurchaseOrderHeaders.Any())
            {
                ModelState.AddModelError(nameof(model.PurchaseRequestId), "This purchase request already has a purchase order.");
            }
            else if (existingHeader is not null &&
                request.PurchaseOrderHeaders.Any(x => x.PurchaseOrderId != existingHeader.PurchaseOrderId))
            {
                ModelState.AddModelError(nameof(model.PurchaseRequestId), "This purchase request is linked to another purchase order.");
            }
        }

        var sourceDetailIds = model.Details
            .SelectMany(x => x.Allocations)
            .SelectMany(x => x.Sources)
            .Where(x => x.PurchaseRequestDetailId.HasValue)
            .Select(x => x.PurchaseRequestDetailId!.Value)
            .Distinct()
            .ToList();
        var sourceDetailMap = sourceDetailIds.Count == 0
            ? new Dictionary<int, PurchaseRequestDetail>()
            : await _context.PurchaseRequestDetails
                .AsNoTracking()
                .Include(x => x.PurchaseRequestHeader)
                .Include(x => x.PurchaseOrderAllocationSources)
                .Where(x => sourceDetailIds.Contains(x.PurchaseRequestDetailId))
                .ToDictionaryAsync(x => x.PurchaseRequestDetailId);

        var itemIds = model.Details.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToList();
        var itemMap = await _context.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId);

        var existingReceivedQty = existingHeader?.PurchaseOrderDetails.ToDictionary(x => x.PurchaseOrderDetailId, x => x.ReceivedQty)
            ?? new Dictionary<int, decimal>();
        var existingAllocationReceivedQty = existingHeader?.PurchaseOrderDetails
            .SelectMany(x => x.PurchaseOrderAllocations)
            .ToDictionary(x => x.PurchaseOrderAllocationId, x => x.ReceivedQty)
            ?? new Dictionary<int, decimal>();
        var existingSourceDetailIds = existingHeader?.PurchaseOrderDetails
            .SelectMany(x => x.PurchaseOrderAllocations)
            .SelectMany(x => x.PurchaseOrderAllocationSources)
            .Select(x => x.PurchaseRequestDetailId)
            .ToHashSet() ?? new HashSet<int>();
        var branchIdSet = (await _context.Branches
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.BranchId)
            .ToListAsync()).ToHashSet();

        decimal subtotal = 0m;
        decimal discount = 0m;

        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            detail.LineNumber = i + 1;

            if (!detail.ItemId.HasValue || !itemMap.TryGetValue(detail.ItemId.Value, out _))
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "Please select a valid item.");
                continue;
            }

            if (detail.PurchaseOrderDetailId.HasValue &&
                existingReceivedQty.TryGetValue(detail.PurchaseOrderDetailId.Value, out var receivedQty) &&
                detail.Qty < receivedQty)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "Quantity cannot be less than already received quantity.");
            }

            detail.ReceivedQty = detail.PurchaseOrderDetailId.HasValue &&
                                 existingReceivedQty.TryGetValue(detail.PurchaseOrderDetailId.Value, out var existingQty)
                ? existingQty
                : 0m;

            var gross = detail.Qty * detail.UnitPrice;
            detail.DiscountAmount = 0m;
            detail.LineTotal = gross;
            detail.Allocations = NormalizeAllocations(detail.Allocations, detail.Qty, model.BranchId);

            var allocationTotal = 0m;
            var allocationBranches = new HashSet<int>();
            for (var j = 0; j < detail.Allocations.Count; j++)
            {
                var allocation = detail.Allocations[j];
                if (!allocation.BranchId.HasValue || !branchIdSet.Contains(allocation.BranchId.Value))
                {
                    ModelState.AddModelError($"Details[{i}].Allocations[{j}].BranchId", "Please select a valid delivery branch.");
                    continue;
                }

                if (!allocationBranches.Add(allocation.BranchId.Value))
                {
                    ModelState.AddModelError($"Details[{i}].Allocations[{j}].BranchId", "Duplicate delivery branches are not allowed for the same PO line.");
                }

                if (allocation.PurchaseOrderAllocationId.HasValue &&
                    existingAllocationReceivedQty.TryGetValue(allocation.PurchaseOrderAllocationId.Value, out var allocationReceivedQty) &&
                    allocation.AllocatedQty < allocationReceivedQty)
                {
                    ModelState.AddModelError($"Details[{i}].Allocations[{j}].AllocatedQty", "Allocated quantity cannot be less than already received quantity for this branch.");
                }

                allocationTotal += allocation.AllocatedQty;

                var sourceTotal = 0m;
                for (var k = 0; k < allocation.Sources.Count; k++)
                {
                    var source = allocation.Sources[k];
                    if (!source.PurchaseRequestDetailId.HasValue)
                    {
                        continue;
                    }

                    if (!sourceDetailMap.TryGetValue(source.PurchaseRequestDetailId.Value, out var sourceDetail))
                    {
                        ModelState.AddModelError($"Details[{i}].Allocations[{j}].Sources[{k}].PurchaseRequestDetailId", "Selected PR source was not found.");
                        continue;
                    }

                    if (sourceDetail.PurchaseRequestHeader?.Status != "Approved" &&
                        sourceDetail.PurchaseRequestHeader?.Status != "ConvertedToPO")
                    {
                        ModelState.AddModelError($"Details[{i}].Allocations[{j}].Sources[{k}].PurchaseRequestDetailId", "PR source must be approved.");
                    }

                    if (sourceDetail.ItemId != detail.ItemId)
                    {
                        ModelState.AddModelError($"Details[{i}].Allocations[{j}].Sources[{k}].PurchaseRequestDetailId", "PR source item must match the PO line item.");
                    }

                    if (sourceDetail.PurchaseRequestHeader?.BranchId != allocation.BranchId)
                    {
                        ModelState.AddModelError($"Details[{i}].Allocations[{j}].Sources[{k}].PurchaseRequestDetailId", "PR source branch must match the delivery branch.");
                    }

                    if (sourceDetail.PurchaseOrderAllocationSources.Any(x => !existingSourceDetailIds.Contains(x.PurchaseRequestDetailId)))
                    {
                        ModelState.AddModelError($"Details[{i}].Allocations[{j}].Sources[{k}].PurchaseRequestDetailId", "One or more PR sources already have a PO.");
                    }

                    source.PurchaseRequestId = sourceDetail.PurchaseRequestId;
                    source.PurchaseRequestNo = sourceDetail.PurchaseRequestHeader?.PRNo ?? source.PurchaseRequestNo;
                    sourceTotal += source.SourceQty;
                }

                if (allocation.Sources.Count > 0 && sourceTotal != allocation.AllocatedQty)
                {
                    ModelState.AddModelError($"Details[{i}].Allocations[{j}].Sources", "PR source quantity must equal delivery allocation quantity.");
                }
            }

            if (detail.Allocations.Count == 0)
            {
                ModelState.AddModelError($"Details[{i}].Allocations", "Please allocate this PO line to at least one branch.");
            }

            if (allocationTotal != detail.Qty)
            {
                ModelState.AddModelError($"Details[{i}].Allocations", "Allocated quantity must equal PO line quantity.");
            }

            subtotal += gross;
        }

        if (model.DiscountAmount > subtotal)
        {
            ModelState.AddModelError(nameof(model.DiscountAmount), "Header discount cannot exceed the subtotal.");
        }

        model.Subtotal = subtotal;
        discount = model.DiscountAmount;
        model.VatType = model.VatType == "NoVAT" ? "NoVAT" : "VAT";
        var taxableAmount = subtotal - discount;
        model.VatAmount = model.VatType == "VAT"
            ? Math.Round(taxableAmount * 0.07m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        model.TotalAmount = taxableAmount + model.VatAmount;

        return ModelState.IsValid;
    }

    private static string GetSubmitBlockedReason(PurchaseOrderHeader order)
    {
        if (order.Status is not ("Draft" or "Rejected"))
        {
            return $"Submit is available only for Draft or Rejected purchase orders. Current status is {order.Status}.";
        }

        if (order.SupplierId <= 0)
        {
            return "Submit is blocked because supplier is not selected.";
        }

        if (!order.PurchaseOrderDetails.Any())
        {
            return "Submit is blocked because no PO lines exist.";
        }

        if (order.PurchaseOrderDetails.Any(x => x.ItemId <= 0 || x.Qty <= 0 || x.UnitPrice < 0))
        {
            return "Submit is blocked because one or more PO lines are incomplete.";
        }

        if (order.PurchaseOrderDetails.Any(x => !x.PurchaseOrderAllocations.Any() ||
            x.PurchaseOrderAllocations.Sum(a => a.AllocatedQty) != x.Qty))
        {
            return "Submit is blocked because delivery allocation must equal PO quantity for every line.";
        }

        return string.Empty;
    }

    private static string GetApproveBlockedReason(PurchaseOrderHeader order)
    {
        if (order.Status != "Submitted")
        {
            return $"Approve is available only for Submitted purchase orders. Current status is {order.Status}.";
        }

        return string.Empty;
    }

    private static string GetCancelBlockedReason(PurchaseOrderHeader order)
    {
        if (order.Status == "Cancelled")
        {
            return "Cancelled purchase orders are read-only.";
        }

        if (order.PurchaseOrderDetails.Any(x => x.ReceivedQty > 0) ||
            order.Status is "PartiallyReceived" or "FullyReceived")
        {
            return "Cancel PO is blocked because receiving already exists.";
        }

        if (order.Status is not ("Draft" or "Submitted" or "Approved" or "Rejected"))
        {
            return $"Cancel PO is available only for Draft, Submitted, Approved, or Rejected purchase orders. Current status is {order.Status}.";
        }

        return string.Empty;
    }

    private static bool CanEditOrder(PurchaseOrderHeader order)
    {
        return order.Status is "Draft" or "Rejected";
    }

    private static string GetRejectBlockedReason(PurchaseOrderHeader order, string? rejectReason)
    {
        if (order.Status != "Submitted")
        {
            return $"Reject is available only for Submitted purchase orders. Current status is {order.Status}.";
        }

        if (string.IsNullOrWhiteSpace(rejectReason))
        {
            return "Reject reason is required.";
        }

        return string.Empty;
    }

    private async Task<bool> TrySaveAsync(string duplicateMessage)
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            ModelState.AddModelError(string.Empty, duplicateMessage);
            return false;
        }
    }

    private static List<PurchaseOrderLineEditorViewModel> NormalizeDetails(IEnumerable<PurchaseOrderLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<PurchaseOrderLineEditorViewModel>())
            .Where(x => x.ItemId.HasValue || x.Qty > 0 || x.UnitPrice > 0 || !string.IsNullOrWhiteSpace(x.Remark))
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                return x;
            })
            .ToList();
    }

    private static PurchaseOrderDetail MapDetailEntity(PurchaseOrderLineEditorViewModel detail)
    {
        return new PurchaseOrderDetail
        {
            LineNumber = detail.LineNumber,
            ItemId = detail.ItemId!.Value,
            Qty = detail.Qty,
            ReceivedQty = detail.ReceivedQty,
            UnitPrice = detail.UnitPrice,
            DiscountAmount = detail.DiscountAmount,
            LineTotal = detail.LineTotal,
            Remark = detail.Remark?.Trim(),
            PurchaseOrderAllocations = detail.Allocations
                .Where(x => x.BranchId.HasValue && x.AllocatedQty > 0)
                .Select(x => new PurchaseOrderAllocation
                {
                    BranchId = x.BranchId!.Value,
                    AllocatedQty = x.AllocatedQty,
                    ReceivedQty = x.ReceivedQty,
                    PurchaseOrderAllocationSources = x.Sources
                        .Where(s => s.PurchaseRequestDetailId.HasValue && s.SourceQty > 0)
                        .Select(s => new PurchaseOrderAllocationSource
                        {
                            PurchaseRequestDetailId = s.PurchaseRequestDetailId!.Value,
                            SourceQty = s.SourceQty
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static List<PurchaseOrderAllocationEditorViewModel> NormalizeAllocations(
        IEnumerable<PurchaseOrderAllocationEditorViewModel>? allocations,
        decimal lineQty,
        int? defaultBranchId)
    {
        var normalized = (allocations ?? Enumerable.Empty<PurchaseOrderAllocationEditorViewModel>())
            .Where(x => x.BranchId.HasValue || x.AllocatedQty > 0 || x.PurchaseOrderAllocationId.HasValue)
            .Select(x =>
            {
                x.AllocatedQty = Math.Max(0m, x.AllocatedQty);
                return x;
            })
            .Where(x => x.AllocatedQty > 0 || x.ReceivedQty > 0)
            .ToList();

        if (normalized.Count == 0 && defaultBranchId.HasValue)
        {
            normalized.Add(new PurchaseOrderAllocationEditorViewModel
            {
                BranchId = defaultBranchId,
                AllocatedQty = lineQty
            });
        }

        return normalized;
    }

    private async Task<List<PurchaseRequestHeader>> GetValidPurchaseRequestsForPoAsync(PurchaseOrderFormViewModel model)
    {
        var requestIds = model.Details
            .SelectMany(x => x.Allocations)
            .SelectMany(x => x.Sources)
            .Where(x => x.PurchaseRequestId.HasValue)
            .Select(x => x.PurchaseRequestId!.Value)
            .Concat(model.PurchaseRequestId.HasValue ? new[] { model.PurchaseRequestId.Value } : Array.Empty<int>())
            .Distinct()
            .ToList();

        if (requestIds.Count == 0)
        {
            return new List<PurchaseRequestHeader>();
        }

        return await _context.PurchaseRequestHeaders
            .Where(x => requestIds.Contains(x.PurchaseRequestId) && x.Status == "Approved")
            .ToListAsync();
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private bool CanAccessOrder(PurchaseOrderHeader order)
    {
        if (CurrentUserCanAccessAllBranches())
        {
            return true;
        }

        var branchId = CurrentBranchId();
        return order.BranchId == branchId ||
            order.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == branchId));
    }

    private static string? NormalizeCancelReason(string? cancelReason)
    {
        return NormalizeReason(cancelReason);
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    private Task<string> GetNextPONumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.PurchaseOrderHeaders.Select(x => x.PONo), prefix, date);
    }

    private async Task<string> EnsurePONumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextPONumberAsync(date)
            : existingNo.Trim();
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
