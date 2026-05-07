using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BizCore.Controllers;

[Authorize]
public class ReceiptsController : CrudControllerBase
{
    private readonly AccountingDbContext _context;
    private readonly CompanyProfileSettings _companyProfile;

    public ReceiptsController(AccountingDbContext context, IOptions<CompanyProfileSettings> companyProfileOptions)
    {
        _context = context;
        _companyProfile = companyProfileOptions.Value;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.ReceiptHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.PaymentHeader)
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
                x.ReceiptNo.Contains(keyword) ||
                (x.Customer != null && (
                    x.Customer.CustomerCode.Contains(keyword) ||
                    x.Customer.CustomerName.Contains(keyword) ||
                    (x.Customer.TaxId != null && x.Customer.TaxId.Contains(keyword)))) ||
                (x.PaymentHeader != null && (
                    x.PaymentHeader.PaymentNo.Contains(keyword) ||
                    (x.PaymentHeader.ReferenceNo != null && x.PaymentHeader.ReferenceNo.Contains(keyword)))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.ReceiptDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.ReceiptDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var receipts = await PaginatedList<ReceiptHeader>.CreateAsync(query
            .OrderByDescending(x => x.ReceiptDate)
            .ThenByDescending(x => x.ReceiptId), page, pageSize);

        return View(receipts);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var receipt = await _context.ReceiptHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.PrintLines)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.IssuedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PaymentHeader)
                .ThenInclude(x => x!.PaymentAllocations)
                    .ThenInclude(x => x.InvoiceHeader)
                        .ThenInclude(x => x!.InvoiceDetails)
                            .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.ReceiptId == id.Value);

        return receipt is null || !CanAccessBranch(receipt.BranchId) ? NotFound() : View(receipt);
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var receipt = await _context.ReceiptHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.PrintLines)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.IssuedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PaymentHeader)
                .ThenInclude(x => x!.PaymentAllocations)
                    .ThenInclude(x => x.InvoiceHeader)
                        .ThenInclude(x => x!.InvoiceDetails)
                            .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.ReceiptId == id.Value);

        if (receipt is null || !CanAccessBranch(receipt.BranchId))
        {
            return NotFound();
        }

        PopulatePrintCompanyViewData(_companyProfile);
        return View(receipt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrintLines(int id, string[]? descriptions, decimal?[]? amounts)
    {
        var receipt = await _context.ReceiptHeaders
            .Include(x => x.PrintLines)
            .FirstOrDefaultAsync(x => x.ReceiptId == id);

        if (receipt is null || !CanAccessBranch(receipt.BranchId))
        {
            return NotFound();
        }

        if (receipt.Status == "Cancelled")
        {
            TempData["ReceiptNotice"] = "Cancelled receipts cannot be updated.";
            return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
        }

        var lines = new List<ReceiptPrintLine>();
        var descriptionValues = descriptions ?? Array.Empty<string>();
        var amountValues = amounts ?? Array.Empty<decimal?>();
        var maxLength = Math.Max(descriptionValues.Length, amountValues.Length);

        for (var index = 0; index < maxLength; index++)
        {
            var description = index < descriptionValues.Length ? descriptionValues[index]?.Trim() : null;
            var amount = index < amountValues.Length ? amountValues[index] : null;
            var hasDescription = !string.IsNullOrWhiteSpace(description);
            var hasAmount = amount.HasValue;

            if (!hasDescription && !hasAmount)
            {
                continue;
            }

            if (!hasDescription || !hasAmount)
            {
                TempData["ReceiptNotice"] = "Each receipt print row must have both description and amount.";
                return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
            }

            lines.Add(new ReceiptPrintLine
            {
                ReceiptId = receipt.ReceiptId,
                LineNumber = lines.Count + 1,
                Description = description!,
                Amount = decimal.Round(amount!.Value, 2, MidpointRounding.AwayFromZero)
            });
        }

        if (lines.Any())
        {
            var total = lines.Sum(x => x.Amount);
            if (total != decimal.Round(receipt.TotalReceivedAmount, 2, MidpointRounding.AwayFromZero))
            {
                TempData["ReceiptNotice"] = $"Receipt print lines must total {receipt.TotalReceivedAmount:N2}.";
                return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
            }
        }

        _context.ReceiptPrintLines.RemoveRange(receipt.PrintLines);
        receipt.PrintItemDescription = null;
        foreach (var line in lines)
        {
            receipt.PrintLines.Add(line);
        }
        receipt.UpdatedDate = DateTime.UtcNow;
        receipt.UpdatedByUserId = CurrentUserId();

        await _context.SaveChangesAsync();

        TempData["ReceiptNotice"] = "Receipt print lines were updated.";
        return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var receipt = await _context.ReceiptHeaders.FirstOrDefaultAsync(x => x.ReceiptId == id);

        if (receipt is null || !CanAccessBranch(receipt.BranchId))
        {
            return NotFound();
        }

        if (receipt.Status != "Issued")
        {
            TempData["ReceiptNotice"] = "Only issued receipts can be cancelled.";
            return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            TempData["ReceiptNotice"] = "กรุณาระบุเหตุผลในการยกเลิกใบเสร็จรับเงิน";
            return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
        }

        var now = DateTime.UtcNow;
        var userId = CurrentUserId();
        receipt.Status = "Cancelled";
        receipt.UpdatedDate = now;
        receipt.UpdatedByUserId = userId;
        receipt.CancelledByUserId = userId;
        receipt.CancelledDate = now;
        receipt.CancelReason = cancelReason.Trim();
        await _context.SaveChangesAsync();

        TempData["ReceiptNotice"] = "Receipt was cancelled. Payment and invoice balances were not changed.";
        return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }
}
