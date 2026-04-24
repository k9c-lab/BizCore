using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class FinancialOverviewController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public FinancialOverviewController(AccountingDbContext context)
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

        var invoiceQuery = _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Where(x => x.Status != "Cancelled" && x.InvoiceDate >= from && x.InvoiceDate <= to);
        var apQuery = _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Where(x => (x.Status == "Approved" || x.Status == "PartiallyReceived" || x.Status == "FullyReceived")
                && x.PODate >= from && x.PODate <= to);

        if (effectiveBranchId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            apQuery = apQuery.Where(x => x.BranchId == effectiveBranchId.Value || x.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == effectiveBranchId.Value)));
        }

        var receivableBaseRows = await invoiceQuery
            .Select(x => new
            {
                x.InvoiceId,
                x.InvoiceNo,
                x.InvoiceDate,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : "-",
                BranchName = x.Branch != null ? x.Branch.BranchName : "No Branch",
                x.TotalAmount,
                x.PaidAmount,
                x.BalanceAmount,
                x.Status
            })
            .ToListAsync();

        var revenueTotal = receivableBaseRows.Sum(x => x.TotalAmount);
        var collectedAr = receivableBaseRows.Sum(x => x.PaidAmount);
        var outstandingAr = receivableBaseRows.Sum(x => x.BalanceAmount);

        var payableBaseRows = await apQuery
            .Select(x => new
            {
                x.PurchaseOrderId,
                x.PONo,
                x.PODate,
                SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "-",
                BranchName = x.Branch != null ? x.Branch.BranchName : "No Branch",
                x.TotalAmount,
                PaidAmount = x.SupplierPayments.Where(p => p.Status == "Posted").Sum(p => (decimal?)p.Amount) ?? 0m,
                x.Status
            })
            .ToListAsync();

        var payableAllRows = payableBaseRows
            .Select(x => new PayableDocumentRowViewModel
            {
                PurchaseOrderId = x.PurchaseOrderId,
                PONo = x.PONo,
                PODate = x.PODate,
                SupplierName = x.SupplierName,
                BranchName = x.BranchName,
                TotalAmount = x.TotalAmount,
                PaidAmount = x.PaidAmount,
                BalanceAmount = Math.Max(x.TotalAmount - x.PaidAmount, 0m),
                Status = x.Status
            })
            .ToList();

        var payableRows = payableAllRows
            .Where(x => x.BalanceAmount > 0m)
            .OrderByDescending(x => x.BalanceAmount)
            .ThenByDescending(x => x.PODate)
            .Take(20)
            .ToList();

        var payableSupplierRows = payableAllRows
            .Where(x => x.BalanceAmount > 0m)
            .GroupBy(x => x.SupplierName)
            .Select(x => new PartyBalanceRowViewModel
            {
                PartyName = x.Key,
                DocumentCount = x.Count(),
                TotalAmount = x.Sum(i => i.TotalAmount),
                PaidAmount = x.Sum(i => i.PaidAmount),
                BalanceAmount = x.Sum(i => i.BalanceAmount)
            })
            .OrderByDescending(x => x.BalanceAmount)
            .Take(15)
            .ToList();

        var purchaseBase = payableAllRows.Sum(x => x.TotalAmount);
        var paidAp = payableAllRows.Sum(x => x.PaidAmount);
        var outstandingAp = payableAllRows.Sum(x => x.BalanceAmount);

        var receivableRows = receivableBaseRows
            .Select(x => new ReceivableDocumentRowViewModel
            {
                InvoiceId = x.InvoiceId,
                InvoiceNo = x.InvoiceNo,
                InvoiceDate = x.InvoiceDate,
                CustomerName = x.CustomerName,
                BranchName = x.BranchName,
                TotalAmount = x.TotalAmount,
                PaidAmount = x.PaidAmount,
                BalanceAmount = x.BalanceAmount,
                Status = x.Status
            })
            .Where(x => x.BalanceAmount > 0m)
            .OrderByDescending(x => x.BalanceAmount)
            .ThenByDescending(x => x.InvoiceDate)
            .Take(20)
            .ToList();

        var receivableCustomerRows = receivableBaseRows
            .Where(x => x.BalanceAmount > 0m)
            .GroupBy(x => x.CustomerName)
            .Select(x => new PartyBalanceRowViewModel
            {
                PartyName = x.Key,
                DocumentCount = x.Count(),
                TotalAmount = x.Sum(i => i.TotalAmount),
                PaidAmount = x.Sum(i => i.PaidAmount),
                BalanceAmount = x.Sum(i => i.BalanceAmount)
            })
            .OrderByDescending(x => x.BalanceAmount)
            .Take(15)
            .ToList();

        var model = new FinancialOverviewViewModel
        {
            DateFrom = from,
            DateTo = to,
            BranchId = effectiveBranchId,
            BranchName = branchName,
            CanAccessAllBranches = canAccessAllBranches,
            BranchOptions = await BuildBranchOptionsAsync(effectiveBranchId, canAccessAllBranches),
            RevenueTotal = revenueTotal,
            CollectedAr = collectedAr,
            OutstandingAr = outstandingAr,
            PurchaseBase = purchaseBase,
            PaidAp = paidAp,
            OutstandingAp = outstandingAp,
            ReceivableRows = receivableRows,
            ReceivableCustomerRows = receivableCustomerRows,
            PayableRows = payableRows,
            PayableSupplierRows = payableSupplierRows
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
