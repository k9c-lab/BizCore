using BizCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public class ReceiptsController : Controller
{
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
}
