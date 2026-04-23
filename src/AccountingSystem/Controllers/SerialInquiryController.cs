using BizCore.Data;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class SerialInquiryController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public SerialInquiryController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int? branchId, int page = 1, int pageSize = 20)
    {
        var canAccessAllBranches = CanAccessAllBranches();
        var effectiveBranchId = ResolveBranchId(branchId, canAccessAllBranches);
        var branchName = await ResolveBranchNameAsync(effectiveBranchId, canAccessAllBranches);

        var query = _context.SerialNumbers
            .AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Supplier)
            .Include(x => x.CurrentCustomer)
            .Include(x => x.InvoiceHeader)
            .Include(x => x.Branch)
            .AsQueryable();

        if (effectiveBranchId.HasValue)
        {
            var selectedBranchId = effectiveBranchId.Value;
            query = query.Where(x => x.BranchId == selectedBranchId);
        }

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
                BranchId = x.BranchId,
                BranchName = x.Branch != null ? x.Branch.BranchName : "-",
                SupplierWarrantyStartDate = x.SupplierWarrantyStartDate,
                SupplierWarrantyEndDate = x.SupplierWarrantyEndDate,
                CustomerWarrantyStartDate = x.CustomerWarrantyStartDate,
                CustomerWarrantyEndDate = x.CustomerWarrantyEndDate,
                CanCustomerClaim = x.Status == "Sold" &&
                    x.CurrentCustomerId.HasValue &&
                    x.InvoiceId.HasValue &&
                    (!x.CustomerWarrantyStartDate.HasValue || x.CustomerWarrantyStartDate.Value.Date <= DateTime.Today) &&
                    (!x.CustomerWarrantyEndDate.HasValue || x.CustomerWarrantyEndDate.Value.Date >= DateTime.Today) &&
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
                            : x.CustomerWarrantyStartDate.HasValue && x.CustomerWarrantyStartDate.Value.Date > DateTime.Today
                                ? "Customer warranty has not started."
                                : x.CustomerWarrantyEndDate.HasValue && x.CustomerWarrantyEndDate.Value.Date < DateTime.Today
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

        var model = new SerialInquiryPageViewModel
        {
            Search = search?.Trim(),
            Status = status,
            BranchId = effectiveBranchId,
            BranchName = branchName,
            CanAccessAllBranches = canAccessAllBranches,
            BranchOptions = await BuildBranchOptionsAsync(effectiveBranchId, canAccessAllBranches),
            Pagination = results.Pagination,
            Results = results.Items
        };

        return View(model);
    }

    private bool CanAccessAllBranches()
    {
        return CurrentUserCanAccessAllBranches();
    }

    private new int? CurrentBranchId()
    {
        return base.CurrentBranchId();
    }

    private int? ResolveBranchId(int? requestedBranchId, bool canAccessAllBranches)
    {
        return canAccessAllBranches ? requestedBranchId : CurrentBranchId();
    }

    private async Task<string> ResolveBranchNameAsync(int? branchId, bool canAccessAllBranches)
    {
        if (!branchId.HasValue && canAccessAllBranches)
        {
            return "All Branches";
        }

        if (!branchId.HasValue)
        {
            return "No Branch";
        }

        return await _context.Branches
            .AsNoTracking()
            .Where(x => x.BranchId == branchId.Value)
            .Select(x => x.BranchName)
            .FirstOrDefaultAsync() ?? "No Branch";
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildBranchOptionsAsync(int? selectedBranchId, bool canAccessAllBranches)
    {
        if (!canAccessAllBranches)
        {
            return Array.Empty<SelectListItem>();
        }

        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "All Branches", Selected = !selectedBranchId.HasValue }
        };

        options.AddRange(await _context.Branches
            .AsNoTracking()
            .OrderBy(x => x.BranchCode)
            .Select(x => new SelectListItem
            {
                Value = x.BranchId.ToString(),
                Text = x.BranchCode + " - " + x.BranchName,
                Selected = selectedBranchId.HasValue && x.BranchId == selectedBranchId.Value
            })
            .ToListAsync());

        return options;
    }
}
