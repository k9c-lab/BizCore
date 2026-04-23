using System.Diagnostics;
using BizCore.Data;
using BizCore.Models;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class HomeController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public HomeController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        var branchId = canAccessAllBranches ? null : CurrentBranchId();
        var branchName = await ResolveBranchNameAsync(branchId, canAccessAllBranches);

        var invoiceQuery = _context.InvoiceHeaders.AsNoTracking().Where(x => x.Status != "Cancelled");
        var paymentQuery = _context.PaymentHeaders.AsNoTracking().Where(x => x.Status == "Posted");
        var stockQuery = _context.StockBalances.AsNoTracking().AsQueryable();
        var poQuery = _context.PurchaseOrderHeaders.AsNoTracking().Where(x => x.Status != "Cancelled");
        var receivingQuery = _context.ReceivingHeaders.AsNoTracking().Where(x => x.Status == "Draft");
        var customerClaimQuery = _context.CustomerClaimHeaders.AsNoTracking().Where(x => x.Status != "Closed" && x.Status != "Cancelled");
        var supplierClaimQuery = _context.SerialClaimLogs.AsNoTracking().Where(x => x.ClaimStatus != "Closed");

        if (branchId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(x => x.BranchId == branchId.Value);
            paymentQuery = paymentQuery.Where(x => x.BranchId == branchId.Value);
            stockQuery = stockQuery.Where(x => x.BranchId == branchId.Value);
            poQuery = poQuery.Where(x => x.BranchId == branchId.Value || x.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == branchId.Value)));
            receivingQuery = receivingQuery.Where(x => x.BranchId == branchId.Value);
            customerClaimQuery = customerClaimQuery.Where(x => x.BranchId == branchId.Value);
            supplierClaimQuery = supplierClaimQuery.Where(x => x.BranchId == branchId.Value);
        }

        var recentInvoices = await invoiceQuery
            .Include(x => x.Customer)
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.InvoiceId)
            .Take(5)
            .Select(x => new DashboardActivityViewModel
            {
                Date = x.InvoiceDate,
                Type = "Invoice",
                DocumentNo = x.InvoiceNo,
                Description = x.Customer != null ? x.Customer.CustomerName : "-",
                Amount = x.TotalAmount,
                Controller = "Invoices",
                ReferenceId = x.InvoiceId
            })
            .ToListAsync();

        var recentPayments = await paymentQuery
            .Include(x => x.Customer)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.PaymentId)
            .Take(5)
            .Select(x => new DashboardActivityViewModel
            {
                Date = x.PaymentDate,
                Type = "Payment",
                DocumentNo = x.PaymentNo,
                Description = x.Customer != null ? x.Customer.CustomerName : "-",
                Amount = x.Amount,
                Controller = "Payments",
                ReferenceId = x.PaymentId
            })
            .ToListAsync();

        var activities = recentInvoices
            .Concat(recentPayments)
            .OrderByDescending(x => x.Date)
            .Take(8)
            .ToList();

        var salesTrendData = await invoiceQuery
            .Where(x => x.InvoiceDate >= today.AddDays(-6) && x.InvoiceDate <= today)
            .GroupBy(x => x.InvoiceDate)
            .Select(x => new
            {
                Date = x.Key,
                Amount = x.Sum(i => i.TotalAmount)
            })
            .ToListAsync();
        var salesTrendMap = salesTrendData.ToDictionary(x => x.Date.Date, x => x.Amount);
        var salesTrend = Enumerable.Range(0, 7)
            .Select(offset => today.AddDays(offset - 6))
            .Select(date => new DashboardChartPointViewModel
            {
                Label = date.ToString("dd MMM"),
                Value = salesTrendMap.TryGetValue(date.Date, out var amount) ? amount : 0m
            })
            .ToList();

        var lowStockItems = await stockQuery
            .Include(x => x.Item)
            .Include(x => x.Branch)
            .Where(x => x.Item != null && x.Item.TrackStock && x.QtyOnHand > 0 && x.QtyOnHand <= 5)
            .OrderBy(x => x.QtyOnHand)
            .ThenBy(x => x.Item!.ItemCode)
            .Take(5)
            .Select(x => new DashboardAttentionViewModel
            {
                Label = x.Item != null ? x.Item.ItemCode : "-",
                Value = x.QtyOnHand.ToString("N2"),
                Detail = (x.Item != null ? x.Item.ItemName : "-") + " / " + (x.Branch != null ? x.Branch.BranchName : "-"),
                Controller = "StockInquiry"
            })
            .ToListAsync();

        var model = new DashboardViewModel
        {
            BranchName = branchName,
            Today = today,
            SalesToday = await invoiceQuery.Where(x => x.InvoiceDate == today).SumAsync(x => (decimal?)x.TotalAmount) ?? 0m,
            SalesThisMonth = await invoiceQuery.Where(x => x.InvoiceDate >= monthStart && x.InvoiceDate <= today).SumAsync(x => (decimal?)x.TotalAmount) ?? 0m,
            PaymentsThisMonth = await paymentQuery.Where(x => x.PaymentDate >= monthStart && x.PaymentDate <= today).SumAsync(x => (decimal?)x.Amount) ?? 0m,
            OutstandingAr = await invoiceQuery.SumAsync(x => (decimal?)x.BalanceAmount) ?? 0m,
            StockOnHandQty = await stockQuery.SumAsync(x => (decimal?)x.QtyOnHand) ?? 0m,
            LowStockItems = await stockQuery.CountAsync(x => x.QtyOnHand > 0 && x.QtyOnHand <= 5),
            OpenPurchaseOrders = await poQuery.CountAsync(x => x.Status == "Approved" || x.Status == "PartiallyReceived"),
            PendingReceivings = await receivingQuery.CountAsync(),
            OpenCustomerClaims = await customerClaimQuery.CountAsync(),
            OpenSupplierClaims = await supplierClaimQuery.CountAsync(x => x.ClaimStatus == "Open" || x.ClaimStatus == "Sent"),
            SalesTrend = salesTrend,
            ClaimMix = new[]
            {
                new DashboardChartPointViewModel { Label = "Customer", Value = await customerClaimQuery.CountAsync() },
                new DashboardChartPointViewModel { Label = "Supplier", Value = await supplierClaimQuery.CountAsync(x => x.ClaimStatus == "Open" || x.ClaimStatus == "Sent") }
            },
            RecentActivities = activities,
            AttentionItems = lowStockItems
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
}
