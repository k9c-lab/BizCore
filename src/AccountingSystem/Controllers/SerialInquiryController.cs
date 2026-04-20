using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,Warehouse")]
public class SerialInquiryController : Controller
{
    private readonly AccountingDbContext _context;

    public SerialInquiryController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        var query = _context.SerialNumbers
            .AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Supplier)
            .Include(x => x.CurrentCustomer)
            .Include(x => x.InvoiceHeader)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.SerialNo.Contains(keyword) ||
                x.Status.Contains(keyword) ||
                (x.Item != null && (
                    x.Item.ItemCode.Contains(keyword) ||
                    x.Item.ItemName.Contains(keyword) ||
                    x.Item.PartNumber.Contains(keyword) ||
                    x.Item.ItemType.Contains(keyword) ||
                    x.Item.Unit.Contains(keyword))) ||
                (x.Supplier != null && (
                    x.Supplier.SupplierCode.Contains(keyword) ||
                    x.Supplier.SupplierName.Contains(keyword) ||
                    (x.Supplier.TaxId != null && x.Supplier.TaxId.Contains(keyword)))) ||
                (x.CurrentCustomer != null && (
                    x.CurrentCustomer.CustomerCode.Contains(keyword) ||
                    x.CurrentCustomer.CustomerName.Contains(keyword) ||
                    (x.CurrentCustomer.TaxId != null && x.CurrentCustomer.TaxId.Contains(keyword)))) ||
                (x.InvoiceHeader != null && (
                    x.InvoiceHeader.InvoiceNo.Contains(keyword) ||
                    (x.InvoiceHeader.ReferenceNo != null && x.InvoiceHeader.ReferenceNo.Contains(keyword)))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var results = await PaginatedList<SerialInquiryRowViewModel>.CreateAsync(query
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
            }), page, pageSize);

        var model = new SerialInquiryPageViewModel
        {
            Search = search?.Trim(),
            Status = status,
            Pagination = results.Pagination,
            Results = results.Items
        };

        return View(model);
    }
}
