using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public class PurchaseOrdersController : CrudControllerBase
{
    private const string NumberPrefix = "PO";
    private readonly AccountingDbContext _context;

    public PurchaseOrdersController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .OrderByDescending(x => x.PODate)
            .ThenByDescending(x => x.PurchaseOrderId)
            .ToListAsync();

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
        ModelState.Remove(nameof(PurchaseOrderFormViewModel.PONo));

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
            Status = model.Status,
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

        if (existingHeader.PurchaseOrderDetails.Any(x => x.ReceivedQty > 0))
        {
            ModelState.AddModelError(string.Empty, "PO lines cannot be edited after receiving has been posted. Create a new PO or continue with receiving only.");
            await PopulateLookupsAsync(model);
            return View(model);
        }

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
        existingHeader.Status = model.Status;
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
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.PurchaseOrderId == id.Value);

        return order is null ? NotFound() : View(order);
    }

    private async Task PopulateLookupsAsync(PurchaseOrderFormViewModel model)
    {
        model.SupplierOptions = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierCode)
            .Select(x => new SelectListItem
            {
                Value = x.SupplierId.ToString(),
                Text = $"{x.SupplierCode} - {x.SupplierName}"
            })
            .ToListAsync();

        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .OrderBy(x => x.ItemCode)
            .Select(x => new QuotationItemLookupViewModel
            {
                ItemId = x.ItemId,
                DisplayText = $"{x.ItemCode} - {x.ItemName}",
                ItemName = x.ItemName,
                UnitPrice = x.UnitPrice,
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
