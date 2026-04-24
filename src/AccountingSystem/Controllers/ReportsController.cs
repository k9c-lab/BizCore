using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class ReportsController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public ReportsController(AccountingDbContext context)
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
            .Include(x => x.Branch)
            .Where(x => x.Status != "Cancelled" && x.InvoiceDate >= from && x.InvoiceDate <= to);
        var paymentQuery = _context.PaymentHeaders
            .AsNoTracking()
            .Where(x => x.Status == "Posted" && x.PaymentDate >= from && x.PaymentDate <= to);
        var supplierPaymentQuery = _context.SupplierPaymentHeaders
            .AsNoTracking()
            .Where(x => x.Status == "Posted" && x.PaymentDate >= from && x.PaymentDate <= to);
        var stockQuery = _context.StockBalances
            .AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Branch)
            .AsQueryable();
        var movementQuery = _context.StockMovements
            .AsNoTracking()
            .Where(x => x.MovementDate >= from && x.MovementDate <= to);
        var customerClaimQuery = _context.CustomerClaimHeaders
            .AsNoTracking()
            .Where(x => x.CustomerClaimDate >= from && x.CustomerClaimDate <= to);
        var supplierClaimQuery = _context.SerialClaimLogs
            .AsNoTracking()
            .Where(x => x.ClaimDate >= from && x.ClaimDate <= to);

        if (effectiveBranchId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            paymentQuery = paymentQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            supplierPaymentQuery = supplierPaymentQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            stockQuery = stockQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            movementQuery = movementQuery.Where(x => x.FromBranchId == effectiveBranchId.Value || x.ToBranchId == effectiveBranchId.Value);
            customerClaimQuery = customerClaimQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            supplierClaimQuery = supplierClaimQuery.Where(x => x.BranchId == effectiveBranchId.Value);
        }

        var salesTotal = await invoiceQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;
        var collectedAr = await paymentQuery.SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var paidAp = await supplierPaymentQuery.SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var salesRows = await invoiceQuery
            .GroupBy(x => new
            {
                x.InvoiceDate,
                BranchName = x.Branch != null ? x.Branch.BranchName : "No Branch"
            })
            .Select(x => new SalesReportRowViewModel
            {
                Date = x.Key.InvoiceDate,
                BranchName = x.Key.BranchName,
                InvoiceCount = x.Count(),
                SalesAmount = x.Sum(i => i.TotalAmount),
                PaidAmount = x.Sum(i => i.PaidAmount),
                BalanceAmount = x.Sum(i => i.BalanceAmount)
            })
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.BranchName)
            .Take(30)
            .ToListAsync();

        var receivableRows = await invoiceQuery
            .Where(x => x.BalanceAmount > 0)
            .Select(x => new ReceivableDocumentRowViewModel
            {
                InvoiceId = x.InvoiceId,
                InvoiceNo = x.InvoiceNo,
                InvoiceDate = x.InvoiceDate,
                CustomerName = x.Customer != null ? x.Customer.CustomerName : "-",
                BranchName = x.Branch != null ? x.Branch.BranchName : "No Branch",
                TotalAmount = x.TotalAmount,
                PaidAmount = x.PaidAmount,
                BalanceAmount = x.BalanceAmount,
                Status = x.Status
            })
            .OrderByDescending(x => x.BalanceAmount)
            .ThenByDescending(x => x.InvoiceDate)
            .Take(20)
            .ToListAsync();

        var receivableCustomerRows = await invoiceQuery
            .Where(x => x.BalanceAmount > 0)
            .GroupBy(x => x.Customer != null ? x.Customer.CustomerName : "No Customer")
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
            .ToListAsync();

        var stockRows = await stockQuery
            .Where(x => x.Item != null && x.Item.TrackStock)
            .OrderByDescending(x => x.QtyOnHand)
            .ThenBy(x => x.Item!.ItemCode)
            .Take(30)
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
            .OrderByDescending(x => x.MovementCount)
            .ToListAsync();

        var customerClaimRows = await customerClaimQuery
            .GroupBy(x => x.Status)
            .Select(x => new ClaimReportRowViewModel
            {
                ClaimType = "Customer",
                Status = x.Key,
                Count = x.Count()
            })
            .ToListAsync();

        var supplierClaimRows = await supplierClaimQuery
            .GroupBy(x => x.ClaimStatus)
            .Select(x => new ClaimReportRowViewModel
            {
                ClaimType = "Supplier",
                Status = x.Key,
                Count = x.Count()
            })
            .ToListAsync();

        var arQuery = _context.InvoiceHeaders.AsNoTracking().Where(x => x.Status != "Cancelled");
        var apQuery = _context.PurchaseOrderHeaders.AsNoTracking()
            .Where(x => x.Status == "Approved" || x.Status == "PartiallyReceived" || x.Status == "FullyReceived");
        if (effectiveBranchId.HasValue)
        {
            arQuery = arQuery.Where(x => x.BranchId == effectiveBranchId.Value);
            apQuery = apQuery.Where(x => x.BranchId == effectiveBranchId.Value || x.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == effectiveBranchId.Value)));
        }

        var outstandingApRows = await apQuery
            .Select(x => new
            {
                x.TotalAmount,
                PaidAmount = x.SupplierPayments.Where(p => p.Status == "Posted").Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync();

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

        var payableRows = payableBaseRows
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
            .Where(x => x.BalanceAmount > 0m)
            .OrderByDescending(x => x.BalanceAmount)
            .ThenByDescending(x => x.PODate)
            .Take(20)
            .ToList();

        var payableSupplierRows = payableRows
            .GroupBy(x => x.SupplierName)
            .Select(x => new PartyBalanceRowViewModel
            {
                PartyName = x.Key,
                DocumentCount = x.Count(),
                TotalAmount = x.Sum(i => i.TotalAmount),
                PaidAmount = x.Sum(i => i.PaidAmount),
                BalanceAmount = x.Sum(i => i.BalanceAmount)
            })
            .Where(x => x.BalanceAmount > 0m)
            .OrderByDescending(x => x.BalanceAmount)
            .Take(15)
            .ToList();

        var model = new ReportsPageViewModel
        {
            DateFrom = from,
            DateTo = to,
            BranchId = effectiveBranchId,
            BranchName = branchName,
            CanAccessAllBranches = canAccessAllBranches,
            BranchOptions = await BuildBranchOptionsAsync(effectiveBranchId, canAccessAllBranches),
            SalesTotal = salesTotal,
            PaymentsTotal = collectedAr,
            SupplierPaymentsTotal = paidAp,
            OutstandingAr = await arQuery.SumAsync(x => (decimal?)x.BalanceAmount) ?? 0m,
            OutstandingAp = outstandingApRows.Sum(x => Math.Max(x.TotalAmount - x.PaidAmount, 0m)),
            CollectedAr = collectedAr,
            PaidAp = paidAp,
            StockQty = await stockQuery.SumAsync(x => (decimal?)x.QtyOnHand) ?? 0m,
            SalesRows = salesRows,
            ReceivableRows = receivableRows,
            ReceivableCustomerRows = receivableCustomerRows,
            PayableRows = payableRows,
            PayableSupplierRows = payableSupplierRows,
            StockRows = stockRows,
            MovementRows = movementRows,
            ClaimRows = customerClaimRows.Concat(supplierClaimRows)
                .OrderBy(x => x.ClaimType)
                .ThenBy(x => x.Status)
                .ToList()
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
