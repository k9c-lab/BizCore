using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public class SerialInquiryController : Controller
{
    private readonly AccountingDbContext _context;

    public SerialInquiryController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? serialNo, string? itemCode, string? partNumber)
    {
        var query = _context.SerialNumbers
            .AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Supplier)
            .Include(x => x.CurrentCustomer)
            .Include(x => x.InvoiceHeader)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(serialNo))
        {
            var trimmed = serialNo.Trim();
            query = query.Where(x => x.SerialNo.Contains(trimmed));
        }

        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            var trimmed = itemCode.Trim();
            query = query.Where(x => x.Item != null && x.Item.ItemCode.Contains(trimmed));
        }

        if (!string.IsNullOrWhiteSpace(partNumber))
        {
            var trimmed = partNumber.Trim();
            query = query.Where(x => x.Item != null && x.Item.PartNumber.Contains(trimmed));
        }

        var model = new SerialInquiryPageViewModel
        {
            SerialNo = serialNo?.Trim(),
            ItemCode = itemCode?.Trim(),
            PartNumber = partNumber?.Trim(),
            Results = await query
                .OrderBy(x => x.SerialNo)
                .Select(x => new SerialInquiryRowViewModel
                {
                    SerialId = x.SerialId,
                    SerialNo = x.SerialNo,
                    ItemCode = x.Item != null ? x.Item.ItemCode : string.Empty,
                    ItemName = x.Item != null ? x.Item.ItemName : string.Empty,
                    PartNumber = x.Item != null ? x.Item.PartNumber : string.Empty,
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
