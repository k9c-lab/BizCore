using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,Warehouse")]
public class PurchaseOrdersController : CrudControllerBase
{
    private const string NumberPrefix = "PO";
    private const string PrintCompanyName = "BizCore Co., Ltd.";
    private const string PrintCompanyAddress = "99 Business Center Road, Huai Khwang, Bangkok 10310";
    private const string PrintCompanyTaxId = "0105559999999";
    private const string PrintCompanyPhone = "02-555-0100";
    private const string PrintCompanyEmail = "sales@bizcore.local";
    private readonly AccountingDbContext _context;

    public PurchaseOrdersController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseOrderDetails)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.PONo.Contains(keyword) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(keyword)) ||
                (x.Supplier != null && (
                    x.Supplier.SupplierCode.Contains(keyword) ||
                    x.Supplier.SupplierName.Contains(keyword) ||
                    (x.Supplier.TaxId != null && x.Supplier.TaxId.Contains(keyword)))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.PODate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.PODate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var orders = await PaginatedList<PurchaseOrderHeader>.CreateAsync(query
            .OrderByDescending(x => x.PODate)
            .ThenByDescending(x => x.PurchaseOrderId), page, pageSize);

        return View(orders);
    }

    public async Task<IActionResult> Create()
    {
        var model = new PurchaseOrderFormViewModel
        {
            PONo = await GetNextPONumberAsync(DateTime.Today)
        };

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderFormViewModel model)
    {
        model.PONo = await EnsurePONumberAsync(model.PONo, model.PODate);
        model.Status = "Draft";
        ModelState.Remove(nameof(PurchaseOrderFormViewModel.PONo));
        ModelState.Remove(nameof(PurchaseOrderFormViewModel.Status));

        if (!await ValidateAndComputeAsync(model, null))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var header = new PurchaseOrderHeader
        {
            PONo = model.PONo,
            PODate = model.PODate,
            SupplierId = model.SupplierId!.Value,
            ReferenceNo = model.ReferenceNo?.Trim(),
            ExpectedReceiveDate = model.ExpectedReceiveDate,
            Remark = model.Remark?.Trim(),
            Subtotal = model.Subtotal,
            DiscountAmount = model.DiscountAmount,
            VatType = model.VatType,
            VatAmount = model.VatAmount,
            TotalAmount = model.TotalAmount,
            Status = "Draft",
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow,
            PurchaseOrderDetails = model.Details.Select(MapDetailEntity).ToList()
        };

        _context.PurchaseOrderHeaders.Add(header);
        if (!await TrySaveAsync("PO number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = header.PurchaseOrderId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var header = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.PurchaseOrderDetails)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id.Value);

        if (header is null)
        {
            return NotFound();
        }

        if (header.Status != "Draft")
        {
            TempData["PurchaseOrderNotice"] = $"Only Draft purchase orders can be edited. Current status is {header.Status}.";
            return RedirectToAction(nameof(Details), new { id = header.PurchaseOrderId });
        }

        var model = new PurchaseOrderFormViewModel
        {
            PurchaseOrderId = header.PurchaseOrderId,
            PONo = header.PONo,
            PODate = header.PODate,
            SupplierId = header.SupplierId,
            ReferenceNo = header.ReferenceNo,
            ExpectedReceiveDate = header.ExpectedReceiveDate,
            Remark = header.Remark,
            Subtotal = header.Subtotal,
            DiscountAmount = header.DiscountAmount,
            VatType = header.VatType,
            VatAmount = header.VatAmount,
            TotalAmount = header.TotalAmount,
            Status = header.Status,
            Details = header.PurchaseOrderDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new PurchaseOrderLineEditorViewModel
                {
                    PurchaseOrderDetailId = x.PurchaseOrderDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    Qty = x.Qty,
                    ReceivedQty = x.ReceivedQty,
                    UnitPrice = x.UnitPrice,
                    DiscountAmount = x.DiscountAmount,
                    LineTotal = x.LineTotal,
                    Remark = x.Remark
                })
                .ToList()
        };

        if (model.Details.Count == 0)
        {
            model.Details.Add(new PurchaseOrderLineEditorViewModel { LineNumber = 1 });
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseOrderFormViewModel model)
    {
        if (id != model.PurchaseOrderId)
        {
            return NotFound();
        }

        var existingHeader = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (existingHeader is null)
        {
            return NotFound();
        }

        if (existingHeader.Status != "Draft")
        {
            TempData["PurchaseOrderNotice"] = $"Only Draft purchase orders can be edited. Current status is {existingHeader.Status}.";
            return RedirectToAction(nameof(Details), new { id = existingHeader.PurchaseOrderId });
        }

        if (existingHeader.PurchaseOrderDetails.Any(x => x.ReceivedQty > 0))
        {
            ModelState.AddModelError(string.Empty, "PO lines cannot be edited after receiving has been posted. Create a new PO or continue with receiving only.");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        model.Status = existingHeader.Status;
        ModelState.Remove(nameof(PurchaseOrderFormViewModel.Status));

        if (!await ValidateAndComputeAsync(model, existingHeader))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        existingHeader.PONo = model.PONo.Trim();
        existingHeader.PODate = model.PODate;
        existingHeader.SupplierId = model.SupplierId!.Value;
        existingHeader.ReferenceNo = model.ReferenceNo?.Trim();
        existingHeader.ExpectedReceiveDate = model.ExpectedReceiveDate;
        existingHeader.Remark = model.Remark?.Trim();
        existingHeader.Subtotal = model.Subtotal;
        existingHeader.DiscountAmount = model.DiscountAmount;
        existingHeader.VatType = model.VatType;
        existingHeader.VatAmount = model.VatAmount;
        existingHeader.TotalAmount = model.TotalAmount;
        existingHeader.UpdatedByUserId = CurrentUserId();
        existingHeader.UpdatedDate = DateTime.UtcNow;

        _context.PurchaseOrderDetails.RemoveRange(existingHeader.PurchaseOrderDetails);
        existingHeader.PurchaseOrderDetails = model.Details.Select(MapDetailEntity).ToList();

        if (!await TrySaveAsync("PO number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = existingHeader.PurchaseOrderId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id.Value);

        return order is null ? NotFound() : View(order);
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var order = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id.Value);

        if (order is null)
        {
            return NotFound();
        }

        ViewData["PrintCompanyName"] = PrintCompanyName;
        ViewData["PrintCompanyAddress"] = PrintCompanyAddress;
        ViewData["PrintCompanyTaxId"] = PrintCompanyTaxId;
        ViewData["PrintCompanyPhone"] = PrintCompanyPhone;
        ViewData["PrintCompanyEmail"] = PrintCompanyEmail;
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var order = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        var blockReason = GetApproveBlockedReason(order);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseOrderNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        order.Status = "Approved";
        order.ApprovedByUserId = CurrentUserId();
        order.ApprovedDate = now;
        order.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseOrderNotice"] = "Purchase order approved successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var order = await _context.PurchaseOrderHeaders
            .Include(x => x.PurchaseOrderDetails)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        var blockReason = GetCancelBlockedReason(order);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["PurchaseOrderNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTime.UtcNow;
        order.Status = "Cancelled";
        order.CancelledByUserId = CurrentUserId();
        order.CancelledDate = now;
        order.CancelReason = NormalizeCancelReason(cancelReason);
        order.UpdatedDate = now;
        await _context.SaveChangesAsync();

        TempData["PurchaseOrderNotice"] = "Purchase order cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateLookupsAsync(PurchaseOrderFormViewModel model)
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierCode)
            .ToListAsync();

        model.SupplierOptions = suppliers
            .Select(x => new SelectListItem($"{x.SupplierCode} - {x.SupplierName}", x.SupplierId.ToString()))
            .ToList();

        model.SupplierLookup = suppliers
            .Select(x => new PurchaseOrderSupplierLookupViewModel
            {
                SupplierId = x.SupplierId,
                SupplierCode = x.SupplierCode,
                SupplierName = x.SupplierName,
                TaxId = x.TaxId ?? string.Empty,
                Phone = x.PhoneNumber ?? string.Empty,
                Email = x.Email ?? string.Empty,
                Address = x.Address ?? string.Empty
            })
            .ToList();

        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .OrderBy(x => x.ItemCode)
            .Select(x => new QuotationItemLookupViewModel
            {
                ItemId = x.ItemId,
                DisplayText = $"{x.ItemCode} - {x.ItemName}",
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                PartNumber = x.PartNumber,
                ItemType = x.ItemType,
                UnitPrice = x.UnitPrice,
                CurrentStock = x.CurrentStock,
                TrackStock = x.TrackStock,
                IsSerialControlled = x.IsSerialControlled
            })
            .ToListAsync();

        model.StatusOptions = new[]
        {
            new SelectListItem("Draft", "Draft"),
            new SelectListItem("Approved", "Approved"),
            new SelectListItem("Cancelled", "Cancelled")
        };

        model.VatTypeOptions = new[]
        {
            new SelectListItem("VAT", "VAT"),
            new SelectListItem("No VAT", "NoVAT")
        };
    }

    private async Task<bool> ValidateAndComputeAsync(PurchaseOrderFormViewModel model, PurchaseOrderHeader? existingHeader)
    {
        model.Details = NormalizeDetails(model.Details);

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "Please add at least one PO line.");
        }

        if (!ModelState.IsValid)
        {
            if (model.Details.Count == 0)
            {
                model.Details.Add(new PurchaseOrderLineEditorViewModel { LineNumber = 1 });
            }

            return false;
        }

        var supplierExists = await _context.Suppliers.AnyAsync(x => x.SupplierId == model.SupplierId);
        if (!supplierExists)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Selected supplier was not found.");
        }

        var itemIds = model.Details.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToList();
        var itemMap = await _context.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId);

        var existingReceivedQty = existingHeader?.PurchaseOrderDetails.ToDictionary(x => x.PurchaseOrderDetailId, x => x.ReceivedQty)
            ?? new Dictionary<int, decimal>();

        decimal subtotal = 0m;
        decimal discount = 0m;

        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            detail.LineNumber = i + 1;

            if (!detail.ItemId.HasValue || !itemMap.TryGetValue(detail.ItemId.Value, out _))
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "Please select a valid item.");
                continue;
            }

            if (detail.PurchaseOrderDetailId.HasValue &&
                existingReceivedQty.TryGetValue(detail.PurchaseOrderDetailId.Value, out var receivedQty) &&
                detail.Qty < receivedQty)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "Quantity cannot be less than already received quantity.");
            }

            detail.ReceivedQty = detail.PurchaseOrderDetailId.HasValue &&
                                 existingReceivedQty.TryGetValue(detail.PurchaseOrderDetailId.Value, out var existingQty)
                ? existingQty
                : 0m;

            var gross = detail.Qty * detail.UnitPrice;
            if (detail.DiscountAmount > gross)
            {
                ModelState.AddModelError($"Details[{i}].DiscountAmount", "Discount cannot exceed the line amount.");
                continue;
            }

            detail.LineTotal = gross - detail.DiscountAmount;
            subtotal += gross;
            discount += detail.DiscountAmount;
        }

        model.Subtotal = subtotal;
        model.DiscountAmount = discount;
        model.VatType = model.VatType == "NoVAT" ? "NoVAT" : "VAT";
        var taxableAmount = subtotal - discount;
        model.VatAmount = model.VatType == "VAT"
            ? Math.Round(taxableAmount * 0.07m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        model.TotalAmount = taxableAmount + model.VatAmount;

        return ModelState.IsValid;
    }

    private static string GetApproveBlockedReason(PurchaseOrderHeader order)
    {
        if (order.Status != "Draft")
        {
            return $"Approve is available only for Draft purchase orders. Current status is {order.Status}.";
        }

        if (order.SupplierId <= 0)
        {
            return "Approve is blocked because supplier is not selected.";
        }

        if (!order.PurchaseOrderDetails.Any())
        {
            return "Approve is blocked because no PO lines exist.";
        }

        if (order.PurchaseOrderDetails.Any(x => x.ItemId <= 0 || x.Qty <= 0 || x.UnitPrice < 0 || x.DiscountAmount < 0))
        {
            return "Approve is blocked because one or more PO lines are incomplete.";
        }

        return string.Empty;
    }

    private static string GetCancelBlockedReason(PurchaseOrderHeader order)
    {
        if (order.Status == "Cancelled")
        {
            return "Cancelled purchase orders are read-only.";
        }

        if (order.PurchaseOrderDetails.Any(x => x.ReceivedQty > 0) ||
            order.Status is "PartiallyReceived" or "FullyReceived")
        {
            return "Cancel PO is blocked because receiving already exists.";
        }

        if (order.Status is not ("Draft" or "Approved"))
        {
            return $"Cancel PO is available only for Draft or Approved purchase orders. Current status is {order.Status}.";
        }

        return string.Empty;
    }

    private async Task<bool> TrySaveAsync(string duplicateMessage)
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            ModelState.AddModelError(string.Empty, duplicateMessage);
            return false;
        }
    }

    private static List<PurchaseOrderLineEditorViewModel> NormalizeDetails(IEnumerable<PurchaseOrderLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<PurchaseOrderLineEditorViewModel>())
            .Where(x => x.ItemId.HasValue || x.Qty > 0 || x.UnitPrice > 0 || x.DiscountAmount > 0 || !string.IsNullOrWhiteSpace(x.Remark))
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                return x;
            })
            .ToList();
    }

    private static PurchaseOrderDetail MapDetailEntity(PurchaseOrderLineEditorViewModel detail)
    {
        return new PurchaseOrderDetail
        {
            LineNumber = detail.LineNumber,
            ItemId = detail.ItemId!.Value,
            Qty = detail.Qty,
            ReceivedQty = detail.ReceivedQty,
            UnitPrice = detail.UnitPrice,
            DiscountAmount = detail.DiscountAmount,
            LineTotal = detail.LineTotal,
            Remark = detail.Remark?.Trim()
        };
    }

    private static string? NormalizeCancelReason(string? cancelReason)
    {
        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            return null;
        }

        var trimmed = cancelReason.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    private Task<string> GetNextPONumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.PurchaseOrderHeaders.Select(x => x.PONo), prefix, date);
    }

    private async Task<string> EnsurePONumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextPONumberAsync(date)
            : existingNo.Trim();
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
