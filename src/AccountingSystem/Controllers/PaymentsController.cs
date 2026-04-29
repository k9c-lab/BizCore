using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class PaymentsController : CrudControllerBase
{
    private const string PaymentNumberPrefix = "PAY";
    private const string ReceiptNumberPrefix = "RC";
    private readonly AccountingDbContext _context;

    public PaymentsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.PaymentHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.ReceiptHeader)
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
                x.PaymentNo.Contains(keyword) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(keyword)) ||
                (x.Customer != null && (
                    x.Customer.CustomerCode.Contains(keyword) ||
                    x.Customer.CustomerName.Contains(keyword) ||
                    (x.Customer.TaxId != null && x.Customer.TaxId.Contains(keyword)))) ||
                (x.ReceiptHeader != null && x.ReceiptHeader.ReceiptNo.Contains(keyword)));
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

        var payments = await PaginatedList<PaymentHeader>.CreateAsync(query
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.PaymentId), page, pageSize);

        return View(payments);
    }

    public async Task<IActionResult> Create(int? customerId, int? invoiceId)
    {
        var model = new PaymentFormViewModel
        {
            PaymentNo = await GetNextPaymentNumberAsync(DateTime.Today),
            BranchId = CurrentBranchId()
        };

        if (invoiceId.HasValue)
        {
            var invoice = await _context.InvoiceHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId.Value);

            if (invoice is null || !CanAccessBranch(invoice.BranchId))
            {
                return NotFound();
            }

            if (!CanReceivePayment(invoice))
            {
                TempData["InvoiceNotice"] = "Payment can only be received for issued or partially paid invoices with an open balance.";
                return RedirectToAction("Details", "Invoices", new { id = invoice.InvoiceId });
            }

            model.CustomerId = invoice.CustomerId;
            model.BranchId = invoice.BranchId;
            model.Amount = invoice.BalanceAmount;
            model.Allocations.Add(new PaymentInvoiceAllocationEditorViewModel
            {
                InvoiceId = invoice.InvoiceId,
                AppliedAmount = invoice.BalanceAmount
            });
        }
        else if (customerId.HasValue)
        {
            model.CustomerId = customerId.Value;
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentFormViewModel model)
    {
        model.PaymentNo = await GetNextPaymentNumberAsync(model.PaymentDate);
        ModelState.Remove(nameof(PaymentFormViewModel.PaymentNo));

        if (!await ValidateAndComputeAsync(model))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var invoiceIds = model.Allocations.Where(x => x.AppliedAmount > 0).Select(x => x.InvoiceId).Distinct().ToList();
            var invoiceMap = await _context.InvoiceHeaders
                .Where(x => invoiceIds.Contains(x.InvoiceId))
                .ToDictionaryAsync(x => x.InvoiceId);
            var branchId = invoiceMap.Values.Select(x => x.BranchId).FirstOrDefault(x => x.HasValue) ?? model.BranchId ?? CurrentBranchId();

            var payment = new PaymentHeader
            {
                PaymentNo = model.PaymentNo,
                PaymentDate = model.PaymentDate.Date,
                CustomerId = model.CustomerId!.Value,
                BranchId = branchId,
                PaymentMethod = model.PaymentMethod,
                ReferenceNo = model.ReferenceNo?.Trim(),
                Amount = model.Amount,
                Remark = model.Remark?.Trim(),
                Status = "Posted",
                CreatedDate = now,
                CreatedByUserId = userId,
                PostedByUserId = userId,
                PostedDate = now,
                PaymentAllocations = model.Allocations
                    .Where(x => x.AppliedAmount > 0)
                    .Select(x => new PaymentAllocation
                    {
                        InvoiceId = x.InvoiceId,
                        AppliedAmount = x.AppliedAmount
                    })
                    .ToList()
            };

            _context.PaymentHeaders.Add(payment);
            await _context.SaveChangesAsync();

            foreach (var allocation in payment.PaymentAllocations)
            {
                var invoice = invoiceMap[allocation.InvoiceId];
                invoice.PaidAmount += allocation.AppliedAmount;
                invoice.BalanceAmount = Math.Max(invoice.TotalAmount - invoice.PaidAmount, 0m);
                invoice.Status = ComputeInvoiceStatus(invoice);
                invoice.UpdatedDate = now;
                invoice.UpdatedByUserId = userId;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Payment number, receipt number, or payment allocation is already in use.");
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var payment = await _context.PaymentHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.PostedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.ReceiptHeader)
            .Include(x => x.PaymentAllocations)
                .ThenInclude(x => x.InvoiceHeader)
            .FirstOrDefaultAsync(x => x.PaymentId == id.Value);

        return payment is null || !CanAccessBranch(payment.BranchId) ? NotFound() : View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateReceipt(int id)
    {
        var payment = await _context.PaymentHeaders
            .Include(x => x.Branch)
            .Include(x => x.ReceiptHeader)
            .FirstOrDefaultAsync(x => x.PaymentId == id);

        if (payment is null || !CanAccessBranch(payment.BranchId))
        {
            return NotFound();
        }

        if (payment.Status != "Posted")
        {
            TempData["PaymentNotice"] = "Receipt can only be generated for posted payments.";
            return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
        }

        if (payment.ReceiptHeader is not null)
        {
            TempData["PaymentNotice"] = "This payment already has a receipt.";
            return RedirectToAction("Details", "Receipts", new { id = payment.ReceiptHeader.ReceiptId });
        }

        var now = DateTime.UtcNow;
        var userId = CurrentUserId();
        var receipt = new ReceiptHeader
        {
            ReceiptNo = await GetNextReceiptNumberAsync(DateTime.Today),
            ReceiptDate = DateTime.Today,
            CustomerId = payment.CustomerId,
            PaymentId = payment.PaymentId,
            BranchId = payment.BranchId,
            TotalReceivedAmount = payment.Amount,
            Remark = payment.Remark,
            Status = "Issued",
            CreatedDate = now,
            CreatedByUserId = userId,
            IssuedByUserId = userId,
            IssuedDate = now
        };

        _context.ReceiptHeaders.Add(receipt);

        try
        {
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Receipts", new { id = receipt.ReceiptId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            TempData["PaymentNotice"] = "This payment already has a receipt.";
            return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var payment = await _context.PaymentHeaders
            .Include(x => x.Branch)
            .Include(x => x.ReceiptHeader)
            .Include(x => x.PaymentAllocations)
            .FirstOrDefaultAsync(x => x.PaymentId == id);

        if (payment is null || !CanAccessBranch(payment.BranchId))
        {
            return NotFound();
        }

        if (payment.Status != "Posted")
        {
            TempData["PaymentNotice"] = "Only posted payments can be cancelled.";
            return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
        }

        if (payment.ReceiptHeader is not null && payment.ReceiptHeader.Status == "Issued")
        {
            TempData["PaymentNotice"] = "Cannot cancel payment because receipt already exists. Cancel receipt first.";
            return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var invoiceIds = payment.PaymentAllocations.Select(x => x.InvoiceId).Distinct().ToList();
            var invoiceMap = await _context.InvoiceHeaders
                .Where(x => invoiceIds.Contains(x.InvoiceId))
                .ToDictionaryAsync(x => x.InvoiceId);

            foreach (var allocation in payment.PaymentAllocations)
            {
                if (!invoiceMap.TryGetValue(allocation.InvoiceId, out var invoice))
                {
                    throw new InvalidOperationException("A payment allocation references an invoice that no longer exists.");
                }

                invoice.PaidAmount = Math.Max(invoice.PaidAmount - allocation.AppliedAmount, 0m);
                invoice.BalanceAmount = Math.Max(invoice.TotalAmount - invoice.PaidAmount, 0m);
                invoice.Status = ComputeInvoiceStatus(invoice);
                invoice.UpdatedDate = now;
                invoice.UpdatedByUserId = userId;
            }

            payment.Status = "Cancelled";
            payment.UpdatedDate = now;
            payment.UpdatedByUserId = userId;
            payment.CancelledByUserId = userId;
            payment.CancelledDate = now;
            payment.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason.Trim();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["PaymentNotice"] = "Payment was cancelled and invoice balances were restored.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["PaymentNotice"] = "Payment cancellation failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id = payment.PaymentId });
    }

    private async Task PopulateLookupsAsync(PaymentFormViewModel model)
    {
        if (!CurrentUserCanAccessAllBranches())
        {
            model.BranchId = CurrentBranchId();
        }

        model.BranchName = model.BranchId.HasValue
            ? await _context.Branches
                .AsNoTracking()
                .Where(x => x.BranchId == model.BranchId.Value)
                .Select(x => x.BranchName)
                .FirstOrDefaultAsync() ?? "No Branch"
            : "All Branches";

        model.CustomerOptions = await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new SelectListItem
            {
                Value = x.CustomerId.ToString(),
                Text = $"{x.CustomerCode} - {x.CustomerName}",
                Selected = model.CustomerId.HasValue && x.CustomerId == model.CustomerId.Value
            })
            .ToListAsync();

        model.PaymentMethodOptions = new[]
        {
            new SelectListItem("Cash", "Cash"),
            new SelectListItem("Transfer", "Transfer"),
            new SelectListItem("Cheque", "Cheque"),
            new SelectListItem("Other", "Other")
        };

        model.CustomerLookup = await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new QuotationCustomerLookupViewModel
            {
                CustomerId = x.CustomerId,
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                TaxId = x.TaxId ?? string.Empty,
                ContactName = string.Empty,
                Phone = x.PhoneNumber ?? string.Empty,
                Email = x.Email ?? string.Empty,
                BillingAddress = x.Address ?? string.Empty,
                ShippingAddress = x.Address ?? string.Empty
            })
            .ToListAsync();

        var openInvoices = await _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.Status != "Cancelled" && x.BalanceAmount > 0)
            .OrderBy(x => x.CustomerId)
            .ThenBy(x => x.InvoiceDate)
            .ThenBy(x => x.InvoiceNo)
            .ToListAsync();

        if (model.BranchId.HasValue)
        {
            openInvoices = openInvoices.Where(x => x.BranchId == model.BranchId.Value).ToList();
        }

        var appliedMap = model.Allocations
            .Where(x => x.AppliedAmount > 0)
            .ToDictionary(x => x.InvoiceId, x => x.AppliedAmount);

        model.Allocations = openInvoices
            .Select(x => new PaymentInvoiceAllocationEditorViewModel
            {
                InvoiceId = x.InvoiceId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer?.CustomerName ?? string.Empty,
                InvoiceNo = x.InvoiceNo,
                InvoiceDate = x.InvoiceDate,
                TotalAmount = x.TotalAmount,
                PaidAmount = x.PaidAmount,
                BalanceAmount = x.BalanceAmount,
                Status = x.Status,
                AppliedAmount = appliedMap.TryGetValue(x.InvoiceId, out var applied) ? applied : 0m
            })
            .ToList();
    }

    private async Task<bool> ValidateAndComputeAsync(PaymentFormViewModel model)
    {
        model.Allocations = NormalizeAllocations(model.Allocations);

        if (!model.CustomerId.HasValue)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Please select a customer.");
        }

        var customerExists = model.CustomerId.HasValue && await _context.Customers.AnyAsync(x => x.CustomerId == model.CustomerId.Value);
        if (model.CustomerId.HasValue && !customerExists)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Selected customer was not found.");
        }

        if (!ModelState.IsValid)
        {
            model.TotalAppliedAmount = 0m;
            model.UnappliedAmount = model.Amount;
            return false;
        }

        var invoiceIds = model.Allocations.Select(x => x.InvoiceId).Distinct().ToList();
        var invoiceMap = await _context.InvoiceHeaders
            .AsNoTracking()
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .ToDictionaryAsync(x => x.InvoiceId);
        var branchIds = invoiceMap.Values.Select(x => x.BranchId).Distinct().ToList();
        var paymentBranchId = branchIds.FirstOrDefault(x => x.HasValue) ?? model.BranchId ?? CurrentBranchId();

        if (paymentBranchId.HasValue && !CanAccessBranch(paymentBranchId))
        {
            ModelState.AddModelError(nameof(model.BranchId), "You cannot post payment for this branch.");
        }
        else if (!paymentBranchId.HasValue && !CurrentUserCanAccessAllBranches())
        {
            ModelState.AddModelError(nameof(model.BranchId), "Payment branch could not be determined.");
        }

        if (branchIds.Count > 1)
        {
            ModelState.AddModelError(nameof(model.Allocations), "All payment allocations must belong to the same branch.");
        }

        model.BranchId = paymentBranchId;

        decimal totalApplied = 0m;

        for (var i = 0; i < model.Allocations.Count; i++)
        {
            var allocation = model.Allocations[i];
            if (!invoiceMap.TryGetValue(allocation.InvoiceId, out var invoice))
            {
                ModelState.AddModelError($"Allocations[{i}].AppliedAmount", "Selected invoice was not found.");
                continue;
            }

            if (invoice.CustomerId != model.CustomerId!.Value)
            {
                ModelState.AddModelError($"Allocations[{i}].AppliedAmount", "Invoice customer must match the payment customer.");
            }

            if (invoice.BranchId != model.BranchId)
            {
                ModelState.AddModelError($"Allocations[{i}].AppliedAmount", "Invoice branch must match the payment branch.");
            }

            if (invoice.Status == "Cancelled")
            {
                ModelState.AddModelError($"Allocations[{i}].AppliedAmount", "Cancelled invoices cannot receive payment.");
            }

            if (invoice.BalanceAmount <= 0)
            {
                ModelState.AddModelError($"Allocations[{i}].AppliedAmount", "Invoice no longer has an open balance.");
            }

            if (allocation.AppliedAmount > invoice.BalanceAmount)
            {
                ModelState.AddModelError($"Allocations[{i}].AppliedAmount", "Applied amount cannot exceed invoice balance.");
            }

            allocation.CustomerId = invoice.CustomerId;
            allocation.CustomerName = string.Empty;
            allocation.InvoiceNo = invoice.InvoiceNo;
            allocation.InvoiceDate = invoice.InvoiceDate;
            allocation.TotalAmount = invoice.TotalAmount;
            allocation.PaidAmount = invoice.PaidAmount;
            allocation.BalanceAmount = invoice.BalanceAmount;
            allocation.Status = invoice.Status;

            totalApplied += allocation.AppliedAmount;
        }

        if (totalApplied > model.Amount)
        {
            ModelState.AddModelError(nameof(model.Amount), "Total applied amount cannot exceed payment amount.");
        }

        model.TotalAppliedAmount = totalApplied;
        model.UnappliedAmount = model.Amount - totalApplied;
        return ModelState.IsValid;
    }

    private static List<PaymentInvoiceAllocationEditorViewModel> NormalizeAllocations(IEnumerable<PaymentInvoiceAllocationEditorViewModel>? allocations)
    {
        return (allocations ?? Enumerable.Empty<PaymentInvoiceAllocationEditorViewModel>())
            .Where(x => x.AppliedAmount > 0)
            .ToList();
    }

    private static string ComputeInvoiceStatus(InvoiceHeader invoice)
    {
        if (invoice.BalanceAmount <= 0)
        {
            return "Paid";
        }

        return invoice.PaidAmount > 0 ? "PartiallyPaid" : "Issued";
    }

    private static bool CanReceivePayment(InvoiceHeader invoice)
    {
        return invoice.BalanceAmount > 0 &&
            (invoice.Status == "Issued" || invoice.Status == "PartiallyPaid");
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private Task<string> GetNextPaymentNumberAsync(DateTime date)
    {
        var prefix = $"{PaymentNumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.PaymentHeaders.Select(x => x.PaymentNo), prefix, PaymentNumberPrefix, date);
    }

    private Task<string> GetNextReceiptNumberAsync(DateTime date)
    {
        var prefix = $"{ReceiptNumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.ReceiptHeaders.Select(x => x.ReceiptNo), prefix, ReceiptNumberPrefix, date);
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, string numberPrefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(numberPrefix, date, nextSequence);
    }
}
