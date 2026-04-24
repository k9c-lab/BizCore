using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class SupplierPaymentsController : CrudControllerBase
{
    private const string PaymentNumberPrefix = "SPAY";
    private readonly AccountingDbContext _context;

    public SupplierPaymentsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        if (!CurrentUserHasPermission("SupplierPayment.View"))
        {
            return Forbid();
        }

        var query = _context.SupplierPaymentHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.PurchaseOrderHeader)
            .AsQueryable();

        if (!CurrentUserCanAccessAllBranches())
        {
            var branchId = CurrentBranchId();
            query = query.Where(x => x.BranchId == branchId || x.PurchaseOrderHeader!.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == branchId)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.PaymentNo.Contains(keyword) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(keyword)) ||
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
            query = query.Where(x => x.PaymentDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.PaymentDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var payments = await PaginatedList<SupplierPaymentHeader>.CreateAsync(query
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.SupplierPaymentId), page, pageSize);

        return View(payments);
    }

    public async Task<IActionResult> Create(int? purchaseOrderId)
    {
        if (!CurrentUserHasPermission("SupplierPayment.Create"))
        {
            return Forbid();
        }

        var model = new SupplierPaymentFormViewModel
        {
            PaymentNo = await GetNextPaymentNumberAsync(DateTime.Today)
        };

        if (purchaseOrderId.HasValue)
        {
            var order = await FindAccessiblePurchaseOrderAsync(purchaseOrderId.Value);
            if (order is null)
            {
                return NotFound();
            }

            var balance = GetOrderBalance(order);
            if (balance <= 0m)
            {
                TempData["PurchaseOrderNotice"] = "This purchase order has already been fully paid.";
                return RedirectToAction("Details", "PurchaseOrders", new { id = purchaseOrderId.Value });
            }

            model.PurchaseOrderId = order.PurchaseOrderId;
            model.Amount = balance;
            ApplyOrderSummary(model, order);
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierPaymentFormViewModel model)
    {
        if (!CurrentUserHasPermission("SupplierPayment.Create"))
        {
            return Forbid();
        }

        model.PaymentNo = await GetNextPaymentNumberAsync(model.PaymentDate);
        ModelState.Remove(nameof(SupplierPaymentFormViewModel.PaymentNo));

        var order = await ValidateAndLoadOrderAsync(model);
        if (order is null || !ModelState.IsValid)
        {
            if (order is not null)
            {
                ApplyOrderSummary(model, order);
            }

            await PopulateLookupsAsync(model);
            return View(model);
        }

        var now = DateTime.UtcNow;
        var userId = CurrentUserId();

        var payment = new SupplierPaymentHeader
        {
            PaymentNo = model.PaymentNo,
            PaymentDate = model.PaymentDate.Date,
            PurchaseOrderId = order.PurchaseOrderId,
            SupplierId = order.SupplierId,
            BranchId = order.BranchId,
            PaymentMethod = model.PaymentMethod,
            ReferenceNo = string.IsNullOrWhiteSpace(model.ReferenceNo) ? null : model.ReferenceNo.Trim(),
            Amount = model.Amount,
            Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim(),
            Status = "Posted",
            CreatedDate = now,
            CreatedByUserId = userId,
            PostedDate = now,
            PostedByUserId = userId
        };

        _context.SupplierPaymentHeaders.Add(payment);

        if (!await TrySaveAsync("Supplier payment number must be unique."))
        {
            ApplyOrderSummary(model, order);
            await PopulateLookupsAsync(model);
            return View(model);
        }

        TempData["SupplierPaymentNotice"] = "Supplier payment was posted successfully.";
        return RedirectToAction(nameof(Details), new { id = payment.SupplierPaymentId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!CurrentUserHasPermission("SupplierPayment.View"))
        {
            return Forbid();
        }

        if (id is null)
        {
            return NotFound();
        }

        var payment = await _context.SupplierPaymentHeaders
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.PostedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseOrderHeader!)
                .ThenInclude(x => x.Supplier)
            .Include(x => x.PurchaseOrderHeader!)
                .ThenInclude(x => x.Branch)
            .Include(x => x.PurchaseOrderHeader!)
                .ThenInclude(x => x.SupplierPayments)
            .FirstOrDefaultAsync(x => x.SupplierPaymentId == id.Value);

        return payment is null || !CanAccessSupplierPayment(payment) ? NotFound() : View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        if (!CurrentUserHasPermission("SupplierPayment.Cancel"))
        {
            return Forbid();
        }

        var payment = await _context.SupplierPaymentHeaders
            .Include(x => x.PurchaseOrderHeader!)
                .ThenInclude(x => x.PurchaseOrderDetails)
                    .ThenInclude(x => x.PurchaseOrderAllocations)
            .FirstOrDefaultAsync(x => x.SupplierPaymentId == id);

        if (payment is null || !CanAccessSupplierPayment(payment))
        {
            return NotFound();
        }

        if (payment.Status != "Posted")
        {
            TempData["SupplierPaymentNotice"] = "Only posted supplier payments can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        payment.Status = "Cancelled";
        payment.UpdatedDate = now;
        payment.UpdatedByUserId = CurrentUserId();
        payment.CancelledDate = now;
        payment.CancelledByUserId = CurrentUserId();
        payment.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason.Trim();

        await _context.SaveChangesAsync();

        TempData["SupplierPaymentNotice"] = "Supplier payment was cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateLookupsAsync(SupplierPaymentFormViewModel model)
    {
        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        var currentBranchId = CurrentBranchId();

        var openOrders = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .Include(x => x.SupplierPayments)
            .Where(x => x.Status == "Approved" || x.Status == "PartiallyReceived" || x.Status == "FullyReceived")
            .Where(x => canAccessAllBranches || x.BranchId == currentBranchId || x.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == currentBranchId)))
            .OrderByDescending(x => x.PODate)
            .ThenByDescending(x => x.PurchaseOrderId)
            .ToListAsync();

        var lookup = openOrders
            .Select(x =>
            {
                var paid = GetOrderPaidAmount(x);
                return new SupplierPaymentPurchaseOrderLookupViewModel
                {
                    PurchaseOrderId = x.PurchaseOrderId,
                    PONo = x.PONo,
                    SupplierCode = x.Supplier?.SupplierCode ?? string.Empty,
                    SupplierName = x.Supplier?.SupplierName ?? "-",
                    BranchId = x.BranchId,
                    BranchName = x.Branch?.BranchName ?? "-",
                    TotalAmount = x.TotalAmount,
                    PaidAmount = paid,
                    BalanceAmount = Math.Max(x.TotalAmount - paid, 0m),
                    Status = x.Status
                };
            })
            .Where(x => x.BalanceAmount > 0m || x.PurchaseOrderId == model.PurchaseOrderId)
            .ToList();

        model.PurchaseOrderLookup = lookup;
        model.PurchaseOrderOptions = lookup
            .Select(x => new SelectListItem($"{x.PONo} - {x.SupplierName}", x.PurchaseOrderId.ToString(), x.PurchaseOrderId == model.PurchaseOrderId))
            .ToList();

        model.PaymentMethodOptions = new[]
        {
            new SelectListItem("Transfer", "Transfer", model.PaymentMethod == "Transfer"),
            new SelectListItem("Cash", "Cash", model.PaymentMethod == "Cash"),
            new SelectListItem("Cheque", "Cheque", model.PaymentMethod == "Cheque")
        };

        if (model.PurchaseOrderId.HasValue)
        {
            var selected = lookup.FirstOrDefault(x => x.PurchaseOrderId == model.PurchaseOrderId.Value);
            if (selected is not null)
            {
                model.SupplierName = selected.SupplierName;
                model.BranchName = selected.BranchName;
                model.PurchaseOrderNo = selected.PONo;
                model.PurchaseOrderTotal = selected.TotalAmount;
                model.PaidAmount = selected.PaidAmount;
                model.BalanceAmount = selected.BalanceAmount;
            }
        }
    }

    private async Task<PurchaseOrderHeader?> ValidateAndLoadOrderAsync(SupplierPaymentFormViewModel model)
    {
        if (!model.PurchaseOrderId.HasValue)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Please select a purchase order.");
            return null;
        }

        var order = await FindAccessiblePurchaseOrderAsync(model.PurchaseOrderId.Value);
        if (order is null)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Selected purchase order was not found or you do not have access.");
            return null;
        }

        if (order.Status is not ("Approved" or "PartiallyReceived" or "FullyReceived"))
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), $"Supplier payment can only be recorded for Approved or received purchase orders. Current status is {order.Status}.");
        }

        var balance = GetOrderBalance(order);
        if (balance <= 0m)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Selected purchase order has already been fully paid.");
        }

        if (model.Amount > balance)
        {
            ModelState.AddModelError(nameof(model.Amount), "Payment amount cannot exceed the remaining payable balance.");
        }

        ApplyOrderSummary(model, order);
        return order;
    }

    private void ApplyOrderSummary(SupplierPaymentFormViewModel model, PurchaseOrderHeader order)
    {
        model.SupplierId = order.SupplierId;
        model.BranchId = order.BranchId;
        model.SupplierName = order.Supplier?.SupplierName ?? "-";
        model.BranchName = order.Branch?.BranchName ?? "-";
        model.PurchaseOrderNo = order.PONo;
        model.PurchaseOrderTotal = order.TotalAmount;
        model.PaidAmount = GetOrderPaidAmount(order);
        model.BalanceAmount = Math.Max(order.TotalAmount - model.PaidAmount, 0m);
    }

    private async Task<PurchaseOrderHeader?> FindAccessiblePurchaseOrderAsync(int purchaseOrderId)
    {
        var order = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Branch)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.PurchaseOrderAllocations)
            .Include(x => x.SupplierPayments)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId);

        return order is not null && CanAccessPurchaseOrder(order) ? order : null;
    }

    private bool CanAccessPurchaseOrder(PurchaseOrderHeader order)
    {
        if (CurrentUserCanAccessAllBranches())
        {
            return true;
        }

        var currentBranchId = CurrentBranchId();
        return order.BranchId == currentBranchId || order.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == currentBranchId));
    }

    private bool CanAccessSupplierPayment(SupplierPaymentHeader payment)
    {
        if (CurrentUserCanAccessAllBranches())
        {
            return true;
        }

        var currentBranchId = CurrentBranchId();
        if (payment.BranchId == currentBranchId)
        {
            return true;
        }

        return payment.PurchaseOrderHeader?.PurchaseOrderDetails.Any(d => d.PurchaseOrderAllocations.Any(a => a.BranchId == currentBranchId)) == true;
    }

    private static decimal GetOrderPaidAmount(PurchaseOrderHeader order)
    {
        return order.SupplierPayments
            .Where(x => x.Status == "Posted")
            .Sum(x => x.Amount);
    }

    private static decimal GetOrderBalance(PurchaseOrderHeader order)
    {
        return Math.Max(order.TotalAmount - GetOrderPaidAmount(order), 0m);
    }

    private Task<string> GetNextPaymentNumberAsync(DateTime date)
    {
        var prefix = $"{PaymentNumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.SupplierPaymentHeaders.Select(x => x.PaymentNo), prefix, PaymentNumberPrefix, date);
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

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, string numberPrefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(numberPrefix, date, nextSequence);
    }
}
