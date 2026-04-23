using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
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

        if (receipt is null || !CanAccessBranch(receipt.BranchId))
        {
            return NotFound();
        }

        if (receipt.Status != "Issued")
        {
            TempData["ReceiptNotice"] = "Only issued receipts can be cancelled.";
            return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
        }

        var now = DateTime.UtcNow;
        var userId = CurrentUserId();
        receipt.Status = "Cancelled";
        receipt.UpdatedDate = now;
        receipt.UpdatedByUserId = userId;
        receipt.CancelledByUserId = userId;
        receipt.CancelledDate = now;
        receipt.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason.Trim();
        await _context.SaveChangesAsync();

        TempData["ReceiptNotice"] = "Receipt was cancelled. Payment and invoice balances were not changed.";
        return RedirectToAction(nameof(Details), new { id = receipt.ReceiptId });
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }
}
