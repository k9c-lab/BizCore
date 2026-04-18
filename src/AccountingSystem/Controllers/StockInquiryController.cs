using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public class StockInquiryController : Controller
{
    private readonly AccountingDbContext _context;

    public StockInquiryController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? itemCode, string? itemName, string? partNumber)
    {
        var query = _context.Items
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            var trimmed = itemCode.Trim();
            query = query.Where(x => x.ItemCode.Contains(trimmed));
        }

        if (!string.IsNullOrWhiteSpace(itemName))
        {
            var trimmed = itemName.Trim();
            query = query.Where(x => x.ItemName.Contains(trimmed));
        }

        if (!string.IsNullOrWhiteSpace(partNumber))
        {
            var trimmed = partNumber.Trim();
            query = query.Where(x => x.PartNumber.Contains(trimmed));
        }

        var model = new StockInquiryPageViewModel
        {
            ItemCode = itemCode?.Trim(),
            ItemName = itemName?.Trim(),
            PartNumber = partNumber?.Trim(),
            Results = await query
                .OrderBy(x => x.ItemCode)
                .Select(x => new StockInquiryRowViewModel
                {
                    ItemId = x.ItemId,
                    ItemCode = x.ItemCode,
                    ItemName = x.ItemName,
                    PartNumber = x.PartNumber,
                    CurrentStock = x.CurrentStock,
                    TrackStock = x.TrackStock,
                    IsSerialControlled = x.IsSerialControlled
                })
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet("StockInquiry/Serials/{itemId:int}")]
    public async Task<IActionResult> Serials(int itemId, string? serialNo)
    {
        var item = await _context.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ItemId == itemId);

        if (item is null)
        {
            return NotFound();
        }

        var serialQuery = _context.SerialNumbers
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.CurrentCustomer)
            .Include(x => x.InvoiceHeader)
            .Where(x => x.ItemId == itemId);

        if (!string.IsNullOrWhiteSpace(serialNo))
        {
            var trimmed = serialNo.Trim();
            serialQuery = serialQuery.Where(x => x.SerialNo.Contains(trimmed));
        }

        var model = new StockInquirySerialsPageViewModel
        {
            ItemId = item.ItemId,
            ItemCode = item.ItemCode,
            ItemName = item.ItemName,
            PartNumber = item.PartNumber,
            CurrentStock = item.CurrentStock,
            SerialNo = serialNo?.Trim(),
            Results = await serialQuery
                .OrderBy(x => x.SerialNo)
                .Select(x => new SerialInquiryRowViewModel
                {
                    SerialId = x.SerialId,
                    SerialNo = x.SerialNo,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    PartNumber = item.PartNumber,
                    Status = x.Status,
                    SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "-",
                    CurrentCustomerName = x.CurrentCustomer != null ? x.CurrentCustomer.CustomerName : "-",
                    InvoiceId = x.InvoiceId,
                    InvoiceCode = x.InvoiceHeader != null ? x.InvoiceHeader.InvoiceNo : "-",
                    SupplierWarrantyStartDate = x.SupplierWarrantyStartDate,
                    SupplierWarrantyEndDate = x.SupplierWarrantyEndDate,
                    CustomerWarrantyStartDate = x.CustomerWarrantyStartDate,
                    CustomerWarrantyEndDate = x.CustomerWarrantyEndDate
                })
                .ToListAsync()
        };

        return View(model);
    }
}
