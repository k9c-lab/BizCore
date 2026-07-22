using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class StockAdjustmentsController : CrudControllerBase
{
    private const string NumberPrefix = "SA";
    private readonly AccountingDbContext _context;

    public StockAdjustmentsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? adjustmentType, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.StockAdjustmentHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.StockAdjustmentDetails)
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
                x.AdjustmentNo.Contains(keyword) ||
                (x.Remark != null && x.Remark.Contains(keyword)) ||
                (x.Branch != null && (x.Branch.BranchCode.Contains(keyword) || x.Branch.BranchName.Contains(keyword))) ||
                x.StockAdjustmentDetails.Any(d => d.Item != null &&
                    (d.Item.ItemCode.Contains(keyword) || d.Item.ItemName.Contains(keyword))));
        }

        if (!string.IsNullOrWhiteSpace(adjustmentType))
        {
            query = query.Where(x => x.AdjustmentType == adjustmentType);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.AdjustmentDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.AdjustmentDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["AdjustmentType"] = adjustmentType;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var adjustments = await PaginatedList<StockAdjustmentHeader>.CreateAsync(query
            .OrderByDescending(x => x.AdjustmentDate)
            .ThenByDescending(x => x.StockAdjustmentId), page, pageSize);

        return View(adjustments);
    }

    public async Task<IActionResult> Create()
    {
        var model = new StockAdjustmentFormViewModel
        {
            AdjustmentNo = await GetNextAdjustmentNumberAsync(DateTime.Today),
            BranchId = CurrentBranchId()
        };

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockAdjustmentFormViewModel model)
    {
        model.AdjustmentNo = await EnsureAdjustmentNumberAsync(model.AdjustmentNo, model.AdjustmentDate);
        ModelState.Remove(nameof(StockAdjustmentFormViewModel.AdjustmentNo));

        if (!await ValidateFormAsync(model))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var activeLines = model.Lines.Where(x => x.ItemId.HasValue).ToList();

        if (activeLines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "กรุณาเพิ่มรายการสินค้าอย่างน้อย 1 รายการ");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var branchId = model.BranchId!.Value;

            var itemIds = activeLines.Select(x => x.ItemId!.Value).ToList();
            var balanceMap = await _context.StockBalances
                .Where(x => x.BranchId == branchId && itemIds.Contains(x.ItemId))
                .ToDictionaryAsync(x => x.ItemId, x => x.QtyOnHand);

            var header = new StockAdjustmentHeader
            {
                AdjustmentNo = model.AdjustmentNo.Trim(),
                AdjustmentDate = model.AdjustmentDate,
                BranchId = branchId,
                AdjustmentType = model.AdjustmentType,
                Remark = model.Remark?.Trim(),
                CreatedByUserId = userId,
                CreatedDate = now
            };

            _context.StockAdjustmentHeaders.Add(header);
            await _context.SaveChangesAsync();

            for (var i = 0; i < activeLines.Count; i++)
            {
                var line = activeLines[i];
                var itemId = line.ItemId!.Value;
                var qtyBefore = balanceMap.TryGetValue(itemId, out var existing) ? existing : 0m;
                var qtyAfter = line.NewQty;
                var qtyDelta = qtyAfter - qtyBefore;

                _context.StockAdjustmentDetails.Add(new StockAdjustmentDetail
                {
                    StockAdjustmentId = header.StockAdjustmentId,
                    LineNumber = i + 1,
                    ItemId = itemId,
                    QtyBefore = qtyBefore,
                    QtyAfter = qtyAfter,
                    Remark = line.Remark?.Trim()
                });

                await AdjustStockBalanceAsync(branchId, itemId, qtyDelta);

                _context.StockMovements.Add(new StockMovement
                {
                    MovementDate = model.AdjustmentDate,
                    MovementType = model.AdjustmentType,
                    ReferenceType = "StockAdjustment",
                    ReferenceId = header.StockAdjustmentId,
                    ItemId = itemId,
                    ToBranchId = branchId,
                    Qty = qtyDelta,
                    Remark = line.Remark?.Trim() ?? model.Remark?.Trim(),
                    CreatedByUserId = userId,
                    CreatedDate = now
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = header.StockAdjustmentId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "เลขที่เอกสารซ้ำ กรุณาตรวจสอบอีกครั้ง");
            await PopulateLookupsAsync(model);
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var adjustment = await _context.StockAdjustmentHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.StockAdjustmentDetails.OrderBy(d => d.LineNumber))
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.StockAdjustmentId == id.Value);

        if (adjustment is null || !CanAccessBranch(adjustment.BranchId))
        {
            return NotFound();
        }

        return View(adjustment);
    }

    [HttpGet]
    public async Task<IActionResult> CurrentStock(int? itemId, int? branchId)
    {
        if (!itemId.HasValue || !branchId.HasValue)
        {
            return Json(new { qtyOnHand = 0m });
        }

        if (!CanAccessBranch(branchId.Value))
        {
            return Forbid();
        }

        var qty = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.ItemId == itemId.Value && x.BranchId == branchId.Value)
            .Select(x => x.QtyOnHand)
            .FirstOrDefaultAsync();

        return Json(new { qtyOnHand = qty });
    }

    private async Task AdjustStockBalanceAsync(int branchId, int itemId, decimal qtyDelta)
    {
        var balance = await _context.StockBalances
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.ItemId == itemId);

        if (balance is null)
        {
            balance = new StockBalance { BranchId = branchId, ItemId = itemId, QtyOnHand = 0 };
            _context.StockBalances.Add(balance);
        }

        balance.QtyOnHand += qtyDelta;
    }

    private async Task<bool> ValidateFormAsync(StockAdjustmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return false;
        }

        if (!model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "กรุณาเลือกสาขา");
            return false;
        }

        if (!CanAccessBranch(model.BranchId.Value))
        {
            ModelState.AddModelError(nameof(model.BranchId), "คุณไม่มีสิทธิ์ปรับสต็อกของสาขานี้");
            return false;
        }

        return await Task.FromResult(true);
    }

    private async Task PopulateLookupsAsync(StockAdjustmentFormViewModel model)
    {
        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        model.CanAccessAllBranches = canAccessAllBranches;

        if (!canAccessAllBranches)
        {
            model.BranchId ??= CurrentBranchId();
        }
        else
        {
            model.BranchOptions = await _context.Branches
                .AsNoTracking()
                .OrderBy(x => x.BranchCode)
                .Select(x => new SelectListItem(x.BranchCode + " - " + x.BranchName, x.BranchId.ToString()))
                .ToListAsync();
        }

        if (model.BranchId.HasValue)
        {
            var branch = await _context.Branches.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == model.BranchId.Value);
            model.BranchName = branch is not null ? $"{branch.BranchCode} - {branch.BranchName}" : string.Empty;
        }

        model.AdjustmentTypeOptions = new List<SelectListItem>
        {
            new("ปรับสต็อก (Adjustment)", "Adjustment"),
            new("สต็อกเปิดบัญชี (Opening Balance)", "OpeningBalance")
        };

        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .Where(x => x.IsActive && x.TrackStock)
            .OrderBy(x => x.ItemCode)
            .Select(x => new QuotationItemLookupViewModel
            {
                ItemId = x.ItemId,
                DisplayText = $"{x.ItemCode} - {x.ItemName}",
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                PartNumber = x.PartNumber ?? string.Empty,
                IsSerialControlled = x.IsSerialControlled
            })
            .ToListAsync();
    }

    private bool CanAccessBranch(int branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private Task<string> GetNextAdjustmentNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.StockAdjustmentHeaders.Select(x => x.AdjustmentNo), prefix, date);
    }

    private async Task<string> EnsureAdjustmentNumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextAdjustmentNumberAsync(date)
            : existingNo.Trim();
    }

    private async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
