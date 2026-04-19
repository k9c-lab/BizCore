using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public class PaymentsController : CrudControllerBase
{
    private const string PaymentNumberPrefix = "PAY";
    private const string ReceiptNumberPrefix = "RC";
    private readonly AccountingDbContext _context;

    public PaymentsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var payments = await _context.PaymentHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.ReceiptHeader)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.PaymentId)
            .ToListAsync();

        return View(payments);
    }

    public async Task<IActionResult> Create()
    {
        var model = new PaymentFormViewModel
        {
            PaymentNo = await GetNextPaymentNumberAsync(DateTime.Today)
        };

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
            var invoiceIds = model.Allocations.Where(x => x.AppliedAmount > 0).Select(x => x.InvoiceId).Distinct().ToList();
            var invoiceMap = await _context.InvoiceHeaders
                .Where(x => invoiceIds.Contains(x.InvoiceId))
                .ToDictionaryAsync(x => x.InvoiceId);

            var payment = new PaymentHeader
            {
                PaymentNo = model.PaymentNo,
                PaymentDate = model.PaymentDate.Date,
                CustomerId = model.CustomerId!.Value,
                PaymentMethod = model.PaymentMethod,
                ReferenceNo = model.ReferenceNo?.Trim(),
                Amount = model.Amount,
                Remark = model.Remark?.Trim(),
                Status = "Posted",
                CreatedDate = DateTime.UtcNow,
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
                invoice.UpdatedDate = DateTime.UtcNow;
            }

            var receipt = new ReceiptHeader
            {
                ReceiptNo = await GetNextReceiptNumberAsync(model.PaymentDate),
                ReceiptDate = model.PaymentDate.Date,
                CustomerId = payment.CustomerId,
                PaymentId = payment.PaymentId,
                TotalReceivedAmount = payment.Amount,
                Remark = payment.Remark,
                Status = "Issued",
                CreatedDate = DateTime.UtcNow
            };

            _context.ReceiptHeaders.Add(receipt);
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
            .Include(x => x.ReceiptHeader)
            .Include(x => x.PaymentAllocations)
                .ThenInclude(x => x.InvoiceHeader)
            .FirstOrDefaultAsync(x => x.PaymentId == id.Value);

        return payment is null ? NotFound() : View(payment);
    }

    private async Task PopulateLookupsAsync(PaymentFormViewModel model)
    {
        model.CustomerOptions = await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new SelectListItem
            {
                Value = x.CustomerId.ToString(),
                Text = $"{x.CustomerCode} - {x.CustomerName}"
            })
            .ToListAsync();

        model.PaymentMethodOptions = new[]
        {
            new SelectListItem("Cash", "Cash"),
            new SelectListItem("Transfer", "Transfer"),
            new SelectListItem("Cheque", "Cheque"),
            new SelectListItem("Other", "Other")
        };

        var openInvoices = await _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.Status != "Cancelled" && x.BalanceAmount > 0)
            .OrderBy(x => x.CustomerId)
            .ThenBy(x => x.InvoiceDate)
            .ThenBy(x => x.InvoiceNo)
            .ToListAsync();

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

        if (model.Allocations.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Allocations), "Please allocate payment to at least one invoice.");
        }

        if (!ModelState.IsValid)
        {
            return false;
        }

        var invoiceIds = model.Allocations.Select(x => x.InvoiceId).Distinct().ToList();
        var invoiceMap = await _context.InvoiceHeaders
            .AsNoTracking()
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .ToDictionaryAsync(x => x.InvoiceId);

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
        model.UnappliedAmount = Math.Max(model.Amount - totalApplied, 0m);
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
