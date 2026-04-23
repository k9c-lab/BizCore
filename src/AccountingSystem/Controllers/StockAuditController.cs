using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,BranchAdmin,Warehouse")]
public class StockAuditController : Controller
{
    private readonly AccountingDbContext _context;

    public StockAuditController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int? branchId, string? auditStatus, int page = 1, int pageSize = 20)
    {
        var canAccessAllBranches = CanAccessAllBranches();
        var effectiveBranchId = ResolveBranchId(branchId, canAccessAllBranches);
        var branchName = await ResolveBranchNameAsync(effectiveBranchId, canAccessAllBranches);

        var itemQuery = _context.Items
            .AsNoTracking()
            .Where(x => x.TrackStock);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            itemQuery = itemQuery.Where(x =>
                x.ItemCode.Contains(keyword) ||
                x.ItemName.Contains(keyword) ||
                x.PartNumber.Contains(keyword));
        }

        var items = await itemQuery
            .OrderBy(x => x.ItemCode)
            .ToListAsync();

        var itemIds = items.Select(x => x.ItemId).ToHashSet();
        var itemMap = items.ToDictionary(x => x.ItemId);

        var branches = await BuildAuditBranchesAsync(effectiveBranchId, canAccessAllBranches);
        var branchIds = branches.Select(x => x.BranchId).ToHashSet();
        var branchMap = branches.ToDictionary(x => x.BranchId);

        var balanceMap = await BuildBalanceMapAsync(itemIds, branchIds);
        var serialMap = await BuildSerialMapAsync(itemIds, branchIds);
        var ledgerMap = await BuildLedgerMapAsync(itemIds, branchIds);

        var keys = balanceMap.Keys
            .Concat(serialMap.Keys)
            .Concat(ledgerMap.Keys)
            .Distinct()
            .Where(x => itemMap.ContainsKey(x.ItemId) && branchMap.ContainsKey(x.BranchId))
            .ToList();

        var rows = keys
            .Select(key => BuildRow(key, itemMap[key.ItemId], branchMap[key.BranchId], balanceMap, serialMap, ledgerMap))
            .OrderByDescending(x => x.IsMismatch)
            .ThenBy(x => x.BranchCode)
            .ThenBy(x => x.ItemCode)
            .ToList();

        if (string.Equals(auditStatus, "Mismatch", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(x => x.IsMismatch).ToList();
        }
        else if (string.Equals(auditStatus, "OK", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(x => !x.IsMismatch).ToList();
        }

        var normalizedPageSize = pageSize is >= 10 and <= 100 ? pageSize : 20;
        var totalCount = rows.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
        var pageIndex = Math.Clamp(page <= 0 ? 1 : page, 1, totalPages);
        var pagedRows = rows
            .Skip((pageIndex - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        var model = new StockAuditPageViewModel
        {
            Search = search?.Trim(),
            BranchId = effectiveBranchId,
            BranchName = branchName,
            AuditStatus = auditStatus,
            CanAccessAllBranches = canAccessAllBranches,
            TotalRows = rows.Count,
            OkRows = rows.Count(x => !x.IsMismatch),
            MismatchRows = rows.Count(x => x.IsMismatch),
            BranchOptions = await BuildBranchOptionsAsync(effectiveBranchId, canAccessAllBranches),
            Pagination = new PaginationViewModel
            {
                PageIndex = pageIndex,
                PageSize = normalizedPageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            },
            Results = pagedRows
        };

        return View(model);
    }

    private static StockAuditRowViewModel BuildRow(
        (int BranchId, int ItemId) key,
        Item item,
        Branch branch,
        IReadOnlyDictionary<(int BranchId, int ItemId), decimal> balanceMap,
        IReadOnlyDictionary<(int BranchId, int ItemId), decimal> serialMap,
        IReadOnlyDictionary<(int BranchId, int ItemId), decimal> ledgerMap)
    {
        var balanceQty = balanceMap.TryGetValue(key, out var balance) ? balance : 0m;
        var serialQty = serialMap.TryGetValue(key, out var serial) ? serial : 0m;
        var ledgerQty = ledgerMap.TryGetValue(key, out var ledger) ? ledger : 0m;
        var balanceLedgerDiff = balanceQty - ledgerQty;
        decimal? balanceSerialDiff = item.IsSerialControlled ? balanceQty - serialQty : null;
        var isMismatch = balanceLedgerDiff != 0m || (balanceSerialDiff.HasValue && balanceSerialDiff.Value != 0m);
        var issues = new List<string>();

        if (balanceLedgerDiff != 0m)
        {
            issues.Add("Balance vs ledger");
        }

        if (balanceSerialDiff.HasValue && balanceSerialDiff.Value != 0m)
        {
            issues.Add("Balance vs serial");
        }

        return new StockAuditRowViewModel
        {
            BranchId = branch.BranchId,
            BranchCode = branch.BranchCode,
            BranchName = branch.BranchName,
            ItemId = item.ItemId,
            ItemCode = item.ItemCode,
            ItemName = item.ItemName,
            PartNumber = item.PartNumber,
            IsSerialControlled = item.IsSerialControlled,
            BalanceQty = balanceQty,
            SerialInStockQty = item.IsSerialControlled ? serialQty : null,
            LedgerNetQty = ledgerQty,
            BalanceLedgerDiff = balanceLedgerDiff,
            BalanceSerialDiff = balanceSerialDiff,
            IsMismatch = isMismatch,
            IssueText = issues.Count == 0 ? "OK" : string.Join(", ", issues)
        };
    }

    private async Task<Dictionary<(int BranchId, int ItemId), decimal>> BuildBalanceMapAsync(
        IReadOnlySet<int> itemIds,
        IReadOnlySet<int> branchIds)
    {
        return await _context.StockBalances
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId) && branchIds.Contains(x.BranchId))
            .GroupBy(x => new { x.BranchId, x.ItemId })
            .Select(x => new
            {
                x.Key.BranchId,
                x.Key.ItemId,
                Qty = x.Sum(b => b.QtyOnHand)
            })
            .ToDictionaryAsync(x => (x.BranchId, x.ItemId), x => x.Qty);
    }

    private async Task<Dictionary<(int BranchId, int ItemId), decimal>> BuildSerialMapAsync(
        IReadOnlySet<int> itemIds,
        IReadOnlySet<int> branchIds)
    {
        return await _context.SerialNumbers
            .AsNoTracking()
            .Where(x => x.BranchId.HasValue &&
                itemIds.Contains(x.ItemId) &&
                branchIds.Contains(x.BranchId.Value) &&
                x.Status == "InStock")
            .GroupBy(x => new { BranchId = x.BranchId!.Value, x.ItemId })
            .Select(x => new
            {
                x.Key.BranchId,
                x.Key.ItemId,
                Qty = (decimal)x.Count()
            })
            .ToDictionaryAsync(x => (x.BranchId, x.ItemId), x => x.Qty);
    }

    private async Task<Dictionary<(int BranchId, int ItemId), decimal>> BuildLedgerMapAsync(
        IReadOnlySet<int> itemIds,
        IReadOnlySet<int> branchIds)
    {
        var movements = await _context.StockMovements
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId) &&
                ((x.FromBranchId.HasValue && branchIds.Contains(x.FromBranchId.Value)) ||
                 (x.ToBranchId.HasValue && branchIds.Contains(x.ToBranchId.Value))))
            .Select(x => new
            {
                x.ItemId,
                x.MovementType,
                x.FromBranchId,
                x.ToBranchId,
                x.Qty
            })
            .ToListAsync();

        var map = new Dictionary<(int BranchId, int ItemId), decimal>();
        foreach (var movement in movements)
        {
            var qty = Math.Abs(movement.Qty);
            switch (movement.MovementType)
            {
                case "Receiving":
                case "InvoiceCancel":
                case "SupplierReplacement":
                case "SupplierRepairReturn":
                    AddLedgerDelta(map, branchIds, movement.ToBranchId, movement.ItemId, qty);
                    break;
                case "ReceivingCancel":
                case "InvoiceIssue":
                case "Issue":
                case "CustomerClaimReplacement":
                    AddLedgerDelta(map, branchIds, movement.FromBranchId, movement.ItemId, -qty);
                    break;
                case "IssueCancel":
                    AddLedgerDelta(map, branchIds, movement.FromBranchId, movement.ItemId, qty);
                    break;
                case "Transfer":
                case "TransferCancel":
                    AddLedgerDelta(map, branchIds, movement.FromBranchId, movement.ItemId, -qty);
                    AddLedgerDelta(map, branchIds, movement.ToBranchId, movement.ItemId, qty);
                    break;
            }
        }

        return map;
    }

    private static void AddLedgerDelta(
        IDictionary<(int BranchId, int ItemId), decimal> map,
        IReadOnlySet<int> branchIds,
        int? branchId,
        int itemId,
        decimal qtyDelta)
    {
        if (!branchId.HasValue || !branchIds.Contains(branchId.Value) || qtyDelta == 0m)
        {
            return;
        }

        var key = (branchId.Value, itemId);
        map[key] = map.TryGetValue(key, out var qty) ? qty + qtyDelta : qtyDelta;
    }

    private async Task<IReadOnlyList<Branch>> BuildAuditBranchesAsync(int? selectedBranchId, bool canAccessAllBranches)
    {
        if (!canAccessAllBranches)
        {
            var branchId = CurrentBranchId();
            return branchId.HasValue
                ? await _context.Branches.AsNoTracking().Where(x => x.BranchId == branchId.Value).ToListAsync()
                : Array.Empty<Branch>();
        }

        var query = _context.Branches.AsNoTracking();
        if (selectedBranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == selectedBranchId.Value);
        }

        return await query.OrderBy(x => x.BranchCode).ToListAsync();
    }

    private bool CanAccessAllBranches()
    {
        return !User.IsInRole("BranchAdmin")
            && (string.Equals(User.FindFirst("CanAccessAllBranches")?.Value, "true", StringComparison.OrdinalIgnoreCase)
                || User.IsInRole("Admin"));
    }

    private int? CurrentBranchId()
    {
        return int.TryParse(User.FindFirst("BranchId")?.Value, out var branchId) ? branchId : null;
    }

    private int? ResolveBranchId(int? requestedBranchId, bool canAccessAllBranches)
    {
        return canAccessAllBranches ? requestedBranchId : CurrentBranchId();
    }

    private async Task<string> ResolveBranchNameAsync(int? branchId, bool canAccessAllBranches)
    {
        if (!branchId.HasValue && canAccessAllBranches)
        {
            return "All Branches";
        }

        if (!branchId.HasValue)
        {
            return "No Branch";
        }

        return await _context.Branches
            .AsNoTracking()
            .Where(x => x.BranchId == branchId.Value)
            .Select(x => x.BranchName)
            .FirstOrDefaultAsync() ?? "No Branch";
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildBranchOptionsAsync(int? selectedBranchId, bool canAccessAllBranches)
    {
        if (!canAccessAllBranches)
        {
            return Array.Empty<SelectListItem>();
        }

        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "All Branches", Selected = !selectedBranchId.HasValue }
        };

        options.AddRange(await _context.Branches
            .AsNoTracking()
            .OrderBy(x => x.BranchCode)
            .Select(x => new SelectListItem
            {
                Value = x.BranchId.ToString(),
                Text = x.BranchCode + " - " + x.BranchName,
                Selected = selectedBranchId.HasValue && x.BranchId == selectedBranchId.Value
            })
            .ToListAsync());

        return options;
    }
}
