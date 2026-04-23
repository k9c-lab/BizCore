using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,BranchAdmin,Warehouse")]
public class PurchaseRequestsController : CrudControllerBase
{
    private const string NumberPrefix = "PR";
    private readonly AccountingDbContext _context;

    public PurchaseRequestsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
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
        if (!await TrySaveAsync("PR number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = header.PurchaseRequestId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
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

        if (header.Status != "Draft")
        {
            TempData["PurchaseRequestNotice"] = $"Only Draft purchase requests can be edited. Current status is {header.Status}.";
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

        if (existingHeader.Status != "Draft")
        {
            TempData["PurchaseRequestNotice"] = $"Only Draft purchase requests can be edited. Current status is {existingHeader.Status}.";
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

        if (!await TrySaveAsync("PR number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = existingHeader.PurchaseRequestId });
    }

    public async Task<IActionResult> Details(int? id)
    {
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
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
        request.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseRequestNotice"] = "Purchase request submitted successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
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
        request.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseRequestNotice"] = "Purchase request approved successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var request = await _context.PurchaseRequestHeaders
            .Include(x => x.PurchaseOrderHeaders)
            .FirstOrDefaultAsync(x => x.PurchaseRequestId == id);

        if (request is null || !CanAccessBranch(request.BranchId))
        {
            return NotFound();
        }

        var blockReason = GetCancelBlockedReason(request);
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

        TempData["PurchaseRequestNotice"] = "Purchase request cancelled successfully.";
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
            ModelState.AddModelError(nameof(model.Details), "Please add at least one PR line.");
        }

        if (!model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Please select a branch.");
        }
        else if (!CanAccessBranch(model.BranchId))
        {
            ModelState.AddModelError(nameof(model.BranchId), "You cannot create or edit purchase requests for this branch.");
        }
        else if (!await _context.Branches.AnyAsync(x => x.BranchId == model.BranchId.Value && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.BranchId), "Selected branch was not found or inactive.");
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
                ModelState.AddModelError($"Details[{i}].ItemId", "Please select a valid item.");
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
        if (request.Status != "Draft")
        {
            return $"Submit is available only for Draft purchase requests. Current status is {request.Status}.";
        }

        if (!request.PurchaseRequestDetails.Any())
        {
            return "Submit is blocked because no PR lines exist.";
        }

        if (request.PurchaseRequestDetails.Any(x => x.ItemId <= 0 || x.RequestedQty <= 0))
        {
            return "Submit is blocked because one or more PR lines are incomplete.";
        }

        return string.Empty;
    }

    private static string GetApproveBlockedReason(PurchaseRequestHeader request)
    {
        if (request.Status != "Submitted")
        {
            return $"Approve is available only for Submitted purchase requests. Current status is {request.Status}.";
        }

        if (!request.PurchaseRequestDetails.Any())
        {
            return "Approve is blocked because no PR lines exist.";
        }

        return string.Empty;
    }

    private static string GetCancelBlockedReason(PurchaseRequestHeader request)
    {
        if (request.Status == "ConvertedToPO" || request.PurchaseOrderHeaders.Any())
        {
            return "Cancel PR is blocked because a purchase order has already been created.";
        }

        if (request.Status == "Cancelled")
        {
            return "Cancelled purchase requests are read-only.";
        }

        if (request.Status is not ("Draft" or "Submitted" or "Approved"))
        {
            return $"Cancel PR is available only for Draft, Submitted, or Approved purchase requests. Current status is {request.Status}.";
        }

        return string.Empty;
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
