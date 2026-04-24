using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class InventoryOverviewController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public InventoryOverviewController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, int? branchId)
    {
        if (!CurrentUserHasPermission("Reports.View"))
        {
            return Forbid();
        }

        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        var effectiveBranchId = canAccessAllBranches ? branchId : CurrentBranchId();
        var from = dateFrom?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = dateTo?.Date ?? DateTime.Today;
        var branchName = await ResolveBranchNameAsync(effectiveBranchId, canAccessAllBranches);

        var stockQuery = _context.StockBalances
            .AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Branch)
            .AsQueryable();
        var movementQuery = _context.StockMovements
            .AsNoTracking()
            .Where(x => x.MovementDate >= from && x.MovementDate <= to);
        var serialQuery = _context.SerialNumbers
            .AsNoTracking()
            .Where(x => x.Status == "InStock");
        var receivingQuery = _context.ReceivingHeaders
            .AsNoTracking()
            .Where(x => x.Status == "Draft");
        var customerClaimQuery = _context.CustomerClaimHeaders
            .AsNoTracking()
            .Where(x => x.Status != "Closed" && x.Status != "Cancelled");
        var supplierClaimQuery = _context.SerialClaimLogs
            .AsNoTracking()
            .Where(x => x.ClaimStatus == "Open" || x.ClaimStatus == "Sent");

        if (effectiveBranchId.HasValue)
        {
            stockQuery = stockQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            movementQuery = movementQuery.Where(x => x.FromBranchId == effectiveBranchId.Value || x.ToBranchId == effectiveBranchId.Value);
            serialQuery = serialQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            receivingQuery = receivingQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            customerClaimQuery = customerClaimQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            supplierClaimQuery = supplierClaimQuery.Where(x => x.BranchId == effectiveBranchId.Value);
        }

        var stockRows = await stockQuery
            .Where(x => x.Item != null && x.Item.TrackStock)
            .Select(x => new StockReportRowViewModel
            {
                BranchName = x.Branch != null ? x.Branch.BranchName : "No Branch",
                ItemCode = x.Item != null ? x.Item.ItemCode : "-",
                ItemName = x.Item != null ? x.Item.ItemName : "-",
                PartNumber = x.Item != null ? x.Item.PartNumber : "-",
                QtyOnHand = x.QtyOnHand
            })
            .ToListAsync();

        var movementRows = await movementQuery
            .GroupBy(x => x.MovementType)
            .Select(x => new MovementReportRowViewModel
            {
                MovementType = x.Key,
                MovementCount = x.Count(),
                Quantity = x.Sum(m => Math.Abs(m.Qty))
            })
            .OrderByDescending(x => x.Quantity)
            .ToListAsync();

        var branchStockRows = await stockQuery
            .GroupBy(x => x.Branch != null ? x.Branch.BranchName : "No Branch")
            .Select(x => new PartyBalanceRowViewModel
            {
                PartyName = x.Key,
                DocumentCount = x.Select(i => i.ItemId).Distinct().Count(),
                TotalAmount = x.Sum(i => i.QtyOnHand),
                PaidAmount = 0m,
                BalanceAmount = x.Sum(i => i.QtyOnHand)
            })
            .OrderByDescending(x => x.BalanceAmount)
            .Take(12)
            .ToListAsync();

        var lowStockRows = stockRows
            .Where(x => x.QtyOnHand > 0m && x.QtyOnHand <= 5m)
            .OrderBy(x => x.QtyOnHand)
            .ThenBy(x => x.ItemCode)
            .Take(15)
            .ToList();

        var topStockRows = stockRows
            .OrderByDescending(x => x.QtyOnHand)
            .ThenBy(x => x.ItemCode)
            .Take(15)
            .ToList();

        var model = new InventoryOverviewViewModel
        {
            DateFrom = from,
            DateTo = to,
            BranchId = effectiveBranchId,
            BranchName = branchName,
            CanAccessAllBranches = canAccessAllBranches,
            BranchOptions = await BuildBranchOptionsAsync(effectiveBranchId, canAccessAllBranches),
            StockOnHandQty = stockRows.Sum(x => x.QtyOnHand),
            InventoryItems = stockRows.Count(x => x.QtyOnHand > 0m),
            LowStockItems = lowStockRows.Count,
            SerialsInStock = await serialQuery.CountAsync(),
            PendingReceivings = await receivingQuery.CountAsync(),
            OpenClaims = await customerClaimQuery.CountAsync() + await supplierClaimQuery.CountAsync(),
            TopStockRows = topStockRows,
            LowStockRows = lowStockRows,
            MovementRows = movementRows,
            BranchStockRows = branchStockRows
        };

        return View(model);
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
