using BizCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,Sales")]
public class ReceiptsController : CrudControllerBase
{
    private const string PrintCompanyName = "BizCore Co., Ltd.";
    private const string PrintCompanyAddress = "99 Business Center Road, Huai Khwang, Bangkok 10310";
    private const string PrintCompanyTaxId = "0105559999999";
    private const string PrintCompanyPhone = "02-555-0100";
    private const string PrintCompanyEmail = "sales@bizcore.local";
    private readonly AccountingDbContext _context;

    public ReceiptsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var receipts = await _context.ReceiptHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.PaymentHeader)
            .OrderByDescending(x => x.ReceiptDate)
            .ThenByDescending(x => x.ReceiptId)
            .ToListAsync();

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
            .Include(x => x.PaymentHeader)
                .ThenInclude(x => x!.PaymentAllocations)
                    .ThenInclude(x => x.InvoiceHeader)
            .FirstOrDefaultAsync(x => x.ReceiptId == id.Value);

        return receipt is null ? NotFound() : View(receipt);
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
            .Include(x => x.CreatedByUser)
            .Include(x => x.IssuedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PaymentHeader)
                .ThenInclude(x => x!.PaymentAllocations)
                    .ThenInclude(x => x.InvoiceHeader)
            .FirstOrDefaultAsync(x => x.ReceiptId == id.Value);

        if (receipt is null)
        {
            return NotFound();
        }

        ViewData["PrintCompanyName"] = PrintCompanyName;
        ViewData["PrintCompanyAddress"] = PrintCompanyAddress;
        ViewData["PrintCompanyTaxId"] = PrintCompanyTaxId;
        ViewData["PrintCompanyPhone"] = PrintCompanyPhone;
        ViewData["PrintCompanyEmail"] = PrintCompanyEmail;
        return View(receipt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var receipt = await _context.ReceiptHeaders.FirstOrDefaultAsync(x => x.ReceiptId == id);

        if (receipt is null)
        {
            return NotFound();
        }

        if (receipt.Status != "Issued")
        {
            TempData["ReceiptNotice"] = "Only issued receipts can be cancelled.";
            return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
        }

        receipt.Status = "Cancelled";
        receipt.UpdatedDate = DateTime.UtcNow;
        receipt.CancelledByUserId = CurrentUserId();
        receipt.CancelledDate = DateTime.UtcNow;
        receipt.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason.Trim();
        await _context.SaveChangesAsync();

        TempData["ReceiptNotice"] = "Receipt was cancelled. Payment and invoice balances were not changed.";
        return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
    }
}
