using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,Warehouse")]
public class StockInquiryController : Controller
{
    private readonly AccountingDbContext _context;

    public StockInquiryController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? itemType, string? status, int page = 1, int pageSize = 20)
    {
        var query = _context.Items
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.ItemCode.Contains(keyword) ||
                x.ItemName.Contains(keyword) ||
                x.PartNumber.Contains(keyword) ||
                x.ItemType.Contains(keyword) ||
                x.Unit.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            query = query.Where(x => x.ItemType == itemType);
        }

        if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.IsActive);
        }
        else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => !x.IsActive);
        }

        var results = await PaginatedList<StockInquiryRowViewModel>.CreateAsync(query
            .OrderBy(x => x.ItemCode)
            .Select(x => new StockInquiryRowViewModel
            {
                ItemId = x.ItemId,
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                PartNumber = x.PartNumber,
                ItemType = x.ItemType,
                Unit = x.Unit,
                CurrentStock = x.CurrentStock,
                TrackStock = x.TrackStock,
                IsSerialControlled = x.IsSerialControlled,
                IsActive = x.IsActive
            }), page, pageSize);

        var model = new StockInquiryPageViewModel
        {
            Search = search?.Trim(),
            ItemType = itemType,
            Status = status,
            Pagination = results.Pagination,
            Results = results.Items
        };

        return View(model);
    }

    [HttpGet("StockInquiry/Serials/{itemId:int}")]
    public async Task<IActionResult> Serials(int itemId, string? search, string? status, int page = 1, int pageSize = 20)
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            serialQuery = serialQuery.Where(x =>
                x.SerialNo.Contains(keyword) ||
                x.Status.Contains(keyword) ||
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
            serialQuery = serialQuery.Where(x => x.Status == status);
        }

        var results = await PaginatedList<SerialInquiryRowViewModel>.CreateAsync(serialQuery
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
                CustomerWarrantyEndDate = x.CustomerWarrantyEndDate,
                CanCustomerClaim = x.Status == "Sold" &&
                    x.CurrentCustomerId.HasValue &&
                    x.InvoiceId.HasValue &&
                    x.CustomerWarrantyStartDate.HasValue &&
                    x.CustomerWarrantyEndDate.HasValue &&
                    x.CustomerWarrantyEndDate.Value.Date >= DateTime.Today &&
                    !_context.CustomerClaimDetails.Any(d => d.SerialId == x.SerialId &&
                        d.CustomerClaimHeader != null &&
                        (d.CustomerClaimHeader.Status == "Open" ||
                         d.CustomerClaimHeader.Status == "Received" ||
                         d.CustomerClaimHeader.Status == "SentToSupplier" ||
                         d.CustomerClaimHeader.Status == "Repairing" ||
                         d.CustomerClaimHeader.Status == "ReadyToReturn" ||
                         d.CustomerClaimHeader.Status == "ReturnedToCustomer")),
                CustomerClaimBlockedReason = x.Status != "Sold"
                    ? "Customer claim is available only for Sold serials."
                    : !x.CurrentCustomerId.HasValue
                        ? "Customer claim is blocked because this serial is not linked to a customer."
                        : !x.InvoiceId.HasValue
                            ? "Customer claim is blocked because this serial is not linked to an invoice."
                            : !x.CustomerWarrantyStartDate.HasValue || !x.CustomerWarrantyEndDate.HasValue
                                ? "Customer warranty is missing."
                                : x.CustomerWarrantyEndDate.Value.Date < DateTime.Today
                                    ? "Customer warranty has expired."
                                    : _context.CustomerClaimDetails.Any(d => d.SerialId == x.SerialId &&
                                        d.CustomerClaimHeader != null &&
                                        (d.CustomerClaimHeader.Status == "Open" ||
                                         d.CustomerClaimHeader.Status == "Received" ||
                                         d.CustomerClaimHeader.Status == "SentToSupplier" ||
                                         d.CustomerClaimHeader.Status == "Repairing" ||
                                         d.CustomerClaimHeader.Status == "ReadyToReturn" ||
                                         d.CustomerClaimHeader.Status == "ReturnedToCustomer"))
                                        ? "This serial already has an open customer claim."
                                        : string.Empty
            }), page, pageSize);

        var model = new StockInquirySerialsPageViewModel
        {
            ItemId = item.ItemId,
            ItemCode = item.ItemCode,
            ItemName = item.ItemName,
            PartNumber = item.PartNumber,
            CurrentStock = item.CurrentStock,
            Search = search?.Trim(),
            Status = status,
            Pagination = results.Pagination,
            Results = results.Items
        };

        return View(model);
    }
}
