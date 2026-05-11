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
public class PurchaseRequestsController : CrudControllerBase
{
    private const string NumberPrefix = "PR";
    private readonly AccountingDbContext _context;
    private readonly PurchaseWorkflowEmailService _purchaseWorkflowEmailService;
    private readonly CompanyProfileSettings _companyProfile;

    public PurchaseRequestsController(
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
        if (!CurrentUserHasPermission("PR.View"))
        {
            return Forbid();
        }

        var query = _context.PurchaseRequestHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.PurchaseRequestDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.PurchaseRequestDetails)
                .ThenInclude(x => x.PurchaseOrderAllocationSources)
                    .ThenInclude(x => x.PurchaseOrderAllocation!)
                        .ThenInclude(x => x.PurchaseOrderDetail!)
                            .ThenInclude(x => x.PurchaseOrderHeader)
            .Include(x => x.PurchaseOrderHeaders)
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
                x.PRNo.Contains(keyword) ||
                (x.Purpose != null && x.Purpose.Contains(keyword)) ||
                (x.Remark != null && x.Remark.Contains(keyword)) ||
                (x.Branch != null && (x.Branch.BranchCode.Contains(keyword) || x.Branch.BranchName.Contains(keyword))) ||
                x.PurchaseRequestDetails.Any(d => d.Item != null &&
                    (d.Item.ItemCode.Contains(keyword) ||
                     d.Item.ItemName.Contains(keyword) ||
                     (d.Item.PartNumber != null && d.Item.PartNumber.Contains(keyword)))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.RequestDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.RequestDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var requests = await PaginatedList<PurchaseRequestHeader>.CreateAsync(query
            .OrderByDescending(x => x.RequestDate)
            .ThenByDescending(x => x.PurchaseRequestId), page, pageSize);

        return View(requests);
    }

    public async Task<IActionResult> Create()
    {
        if (!CurrentUserHasPermission("PR.Create"))
        {
            return Forbid();
        }

        var model = new PurchaseRequestFormViewModel
        {
            PRNo = await GetNextPRNumberAsync(DateTime.Today),
            BranchId = CurrentBranchId()
        };

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseRequestFormViewModel model)
    {
        if (!CurrentUserHasPermission("PR.Create"))
        {
            return Forbid();
        }

        model.PRNo = await EnsurePRNumberAsync(model.PRNo, model.RequestDate);
        model.Status = "Draft";
        ModelState.Remove(nameof(PurchaseRequestFormViewModel.PRNo));
        ModelState.Remove(nameof(PurchaseRequestFormViewModel.Status));

        if (!await ValidateAndComputeAsync(model))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var header = new PurchaseRequestHeader
        {
            PRNo = model.PRNo,
            RequestDate = model.RequestDate,
            RequiredDate = model.RequiredDate,
            BranchId = model.BranchId,
            Purpose = model.Purpose?.Trim(),
            Remark = model.Remark?.Trim(),
            Status = "Draft",
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow,
            PurchaseRequestDetails = model.Details.Select(MapDetailEntity).ToList()
        };

        _context.PurchaseRequestHeaders.Add(header);
        if (!await TrySaveAsync("เลขที่ใบขอซื้อต้องไม่ซ้ำ"))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = header.PurchaseRequestId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (!CurrentUserHasPermission("PR.Edit"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var header = await _context.PurchaseRequestHeaders
            .AsNoTracking()
            .Include(x => x.PurchaseRequestDetails)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id.Value);

        if (header is null || !CanAccessBranch(header.BranchId))
        {
            return NotFound();
        }

        if (!CanEditRequest(header))
        {
            TempData["PurchaseRequestNotice"] = $"แก้ไขได้เฉพาะใบขอซื้อสถานะ Draft หรือ Rejected เท่านั้น สถานะปัจจุบันคือ {header.Status}";
            return RedirectToAction(nameof(Details), new { id = header.PurchaseRequestId });
        }

        var model = new PurchaseRequestFormViewModel
        {
            PurchaseRequestId = header.PurchaseRequestId,
            PRNo = header.PRNo,
            RequestDate = header.RequestDate,
            RequiredDate = header.RequiredDate,
            BranchId = header.BranchId,
            Purpose = header.Purpose,
            Remark = header.Remark,
            Status = header.Status,
            Details = header.PurchaseRequestDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new PurchaseRequestLineEditorViewModel
                {
                    PurchaseRequestDetailId = x.PurchaseRequestDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    RequestedQty = x.RequestedQty,
                    Remark = x.Remark
                })
                .ToList()
        };

        if (model.Details.Count == 0)
        {
            model.Details.Add(new PurchaseRequestLineEditorViewModel { LineNumber = 1 });
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseRequestFormViewModel model)
    {
        if (!CurrentUserHasPermission("PR.Edit"))
        {
            return Forbid();
        }

        if (id != model.PurchaseRequestId)
        {
            return NotFound();
        }

        var existingHeader = await _context.PurchaseRequestHeaders
            .Include(x => x.PurchaseRequestDetails)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id);

        if (existingHeader is null || !CanAccessBranch(existingHeader.BranchId))
        {
            return NotFound();
        }

        if (!CanEditRequest(existingHeader))
        {
            TempData["PurchaseRequestNotice"] = $"แก้ไขได้เฉพาะใบขอซื้อสถานะ Draft หรือ Rejected เท่านั้น สถานะปัจจุบันคือ {existingHeader.Status}";
            return RedirectToAction(nameof(Details), new { id = existingHeader.PurchaseRequestId });
        }

        model.Status = existingHeader.Status;
        ModelState.Remove(nameof(PurchaseRequestFormViewModel.Status));

        if (!await ValidateAndComputeAsync(model))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        existingHeader.PRNo = model.PRNo.Trim();
        existingHeader.RequestDate = model.RequestDate;
        existingHeader.RequiredDate = model.RequiredDate;
        existingHeader.BranchId = model.BranchId;
        existingHeader.Purpose = model.Purpose?.Trim();
        existingHeader.Remark = model.Remark?.Trim();
        existingHeader.UpdatedByUserId = CurrentUserId();
        existingHeader.UpdatedDate = DateTime.UtcNow;

        _context.PurchaseRequestDetails.RemoveRange(existingHeader.PurchaseRequestDetails);
        existingHeader.PurchaseRequestDetails = model.Details.Select(MapDetailEntity).ToList();

        if (!await TrySaveAsync("เลขที่ใบขอซื้อต้องไม่ซ้ำ"))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = existingHeader.PurchaseRequestId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!CurrentUserHasPermission("PR.View"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var request = await _context.PurchaseRequestHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.RejectedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseRequestDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.PurchaseRequestDetails)
                .ThenInclude(x => x.PurchaseOrderAllocationSources)
                    .ThenInclude(x => x.PurchaseOrderAllocation!)
                        .ThenInclude(x => x.PurchaseOrderDetail!)
                            .ThenInclude(x => x.PurchaseOrderHeader!)
                                .ThenInclude(x => x.Supplier)
            .Include(x => x.PurchaseOrderHeaders)
                .ThenInclude(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id.Value);

        return request is null || !CanAccessBranch(request.BranchId) ? NotFound() : View(request);
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (!CurrentUserHasPermission("PR.View"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var request = await _context.PurchaseRequestHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.RejectedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseRequestDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.PurchaseOrderHeaders)
                .ThenInclude(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id.Value);

        if (request is null || !CanAccessBranch(request.BranchId))
        {
            return NotFound();
        }

        PopulatePrintCompanyViewData(_companyProfile);
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        if (!CurrentUserHasPermission("PR.Submit"))
        {
            return Forbid();
        }

        var request = await _context.PurchaseRequestHeaders
            .Include(x => x.PurchaseRequestDetails)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id);

        if (request is null || !CanAccessBranch(request.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetSubmitBlockedReason(request);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseRequestNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        request.Status = "Submitted";
        request.SubmittedByUserId = CurrentUserId();
        request.SubmittedDate = now;
        request.RejectedByUserId = null;
        request.RejectedDate = null;
        request.RejectReason = null;
        request.UpdatedDate = now;
        await _context.SaveChangesAsync();
        await _purchaseWorkflowEmailService.NotifyPrSubmittedAsync(request.PurchaseRequestId);

        TempData["PurchaseRequestNotice"] = "ส่งใบขอซื้อเข้ากระบวนการอนุมัติเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        if (!CurrentUserHasPermission("PR.Approve"))
        {
            return Forbid();
        }

        var request = await _context.PurchaseRequestHeaders
            .Include(x => x.PurchaseRequestDetails)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id);

        if (request is null)
        {
            return NotFound();
        }

        var blockReason = GetApproveBlockedReason(request);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseRequestNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        request.Status = "Approved";
        request.ApprovedByUserId = CurrentUserId();
        request.ApprovedDate = now;
        request.RejectedByUserId = null;
        request.RejectedDate = null;
        request.RejectReason = null;
        request.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseRequestNotice"] = "อนุมัติใบขอซื้อเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? rejectReason)
    {
        if (!CurrentUserHasPermission("PR.Reject"))
        {
            return Forbid();
        }

        var request = await _context.PurchaseRequestHeaders
            .Include(x => x.PurchaseRequestDetails)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id);

        if (request is null || !CanAccessBranch(request.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetRejectBlockedReason(request, rejectReason);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseRequestNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        request.Status = "Rejected";
        request.RejectedByUserId = CurrentUserId();
        request.RejectedDate = now;
        request.RejectReason = NormalizeReason(rejectReason);
        request.ApprovedByUserId = null;
        request.ApprovedDate = null;
        request.UpdatedDate = now;
        await _context.SaveChangesAsync();
        await _purchaseWorkflowEmailService.NotifyPrRejectedAsync(request.PurchaseRequestId);

        TempData["PurchaseRequestNotice"] = "ตีกลับใบขอซื้อเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        if (!CurrentUserHasPermission("PR.Cancel"))
        {
            return Forbid();
        }

        var request = await _context.PurchaseRequestHeaders
            .Include(x => x.PurchaseOrderHeaders)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id);

        if (request is null || !CanAccessBranch(request.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetCancelBlockedReason(request, cancelReason);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseRequestNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        request.Status = "Cancelled";
        request.CancelledByUserId = CurrentUserId();
        request.CancelledDate = now;
        request.CancelReason = NormalizeCancelReason(cancelReason);
        request.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseRequestNotice"] = "ยกเลิกใบขอซื้อเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateLookupsAsync(PurchaseRequestFormViewModel model)
    {
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
    }

    private async Task<bool> ValidateAndComputeAsync(PurchaseRequestFormViewModel model)
    {
        model.Details = NormalizeDetails(model.Details);

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "กรุณาเพิ่มรายการอย่างน้อย 1 รายการ");
        }

        if (!model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "กรุณาเลือกสาขา");
        }
        else if (!CanAccessBranch(model.BranchId))
        {
            ModelState.AddModelError(nameof(model.BranchId), "คุณไม่มีสิทธิ์สร้างหรือแก้ไขใบขอซื้อของสาขานี้");
        }
        else if (!await _context.Branches.AnyAsync(x => x.BranchId == model.BranchId.Value && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.BranchId), "ไม่พบสาขาที่เลือก หรือสาขาถูกปิดใช้งาน");
        }

        var itemIds = model.Details.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToList();
        var itemMap = await _context.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId);

        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            detail.LineNumber = i + 1;

            if (!detail.ItemId.HasValue || !itemMap.ContainsKey(detail.ItemId.Value))
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "กรุณาเลือกรายการสินค้าให้ถูกต้อง");
            }
        }

        if (model.Details.Count == 0)
        {
            model.Details.Add(new PurchaseRequestLineEditorViewModel { LineNumber = 1 });
        }

        return ModelState.IsValid;
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

    private static List<PurchaseRequestLineEditorViewModel> NormalizeDetails(IEnumerable<PurchaseRequestLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<PurchaseRequestLineEditorViewModel>())
            .Where(x => x.ItemId.HasValue || x.RequestedQty > 0 || !string.IsNullOrWhiteSpace(x.Remark))
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                return x;
            })
            .ToList();
    }

    private static PurchaseRequestDetail MapDetailEntity(PurchaseRequestLineEditorViewModel detail)
    {
        return new PurchaseRequestDetail
        {
            LineNumber = detail.LineNumber,
            ItemId = detail.ItemId!.Value,
            RequestedQty = detail.RequestedQty,
            Remark = detail.Remark?.Trim()
        };
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private static string GetSubmitBlockedReason(PurchaseRequestHeader request)
    {
        if (request.Status is not ("Draft" or "Rejected"))
        {
            return $"ส่งอนุมัติได้เฉพาะใบขอซื้อสถานะ Draft หรือ Rejected เท่านั้น สถานะปัจจุบันคือ {request.Status}";
        }

        if (!request.PurchaseRequestDetails.Any())
        {
            return "ยังส่งอนุมัติไม่ได้ เพราะยังไม่มีรายการสินค้า";
        }

        if (request.PurchaseRequestDetails.Any(x => x.ItemId <= 0 || x.RequestedQty <= 0))
        {
            return "ยังส่งอนุมัติไม่ได้ เพราะมีบางรายการยังกรอกข้อมูลไม่ครบ";
        }

        return string.Empty;
    }

    private static string GetApproveBlockedReason(PurchaseRequestHeader request)
    {
        if (request.Status != "Submitted")
        {
            return $"อนุมัติได้เฉพาะใบขอซื้อสถานะ Submitted เท่านั้น สถานะปัจจุบันคือ {request.Status}";
        }

        if (!request.PurchaseRequestDetails.Any())
        {
            return "ยังอนุมัติไม่ได้ เพราะยังไม่มีรายการสินค้า";
        }

        return string.Empty;
    }

    private static string GetCancelBlockedReason(PurchaseRequestHeader request, string? cancelReason)
    {
        if (request.Status == "ConvertedToPO" || request.PurchaseOrderHeaders.Any())
        {
            return "ยังยกเลิกไม่ได้ เพราะมีการสร้างใบสั่งซื้อจากใบขอซื้อนี้แล้ว";
        }

        if (request.Status == "Cancelled")
        {
            return "ใบขอซื้อที่ยกเลิกแล้วไม่สามารถแก้ไขต่อได้";
        }

        if (request.Status is not ("Draft" or "Submitted" or "Approved" or "Rejected"))
        {
            return $"ยกเลิกได้เฉพาะใบขอซื้อสถานะ Draft, Submitted, Approved หรือ Rejected เท่านั้น สถานะปัจจุบันคือ {request.Status}";
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            return "กรุณาระบุเหตุผลที่ยกเลิก";
        }

        return string.Empty;
    }

    private static bool CanEditRequest(PurchaseRequestHeader request)
    {
        return request.Status is "Draft" or "Rejected";
    }

    private static string GetRejectBlockedReason(PurchaseRequestHeader request, string? rejectReason)
    {
        if (request.Status != "Submitted")
        {
            return $"ตีกลับได้เฉพาะใบขอซื้อสถานะ Submitted เท่านั้น สถานะปัจจุบันคือ {request.Status}";
        }

        if (string.IsNullOrWhiteSpace(rejectReason))
        {
            return "กรุณาระบุเหตุผลที่ตีกลับ";
        }

        return string.Empty;
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

    private Task<string> GetNextPRNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.PurchaseRequestHeaders.Select(x => x.PRNo), prefix, date);
    }

    private async Task<string> EnsurePRNumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextPRNumberAsync(date)
            : existingNo.Trim();
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
