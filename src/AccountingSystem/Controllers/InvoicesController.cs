using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public class InvoicesController : CrudControllerBase
{
    private const string NumberPrefix = "INV";
    private const string PrintCompanyName = "BizCore Co., Ltd.";
    private const string PrintCompanyAddress = "99 Business Center Road, Huai Khwang, Bangkok 10310";
    private const string PrintCompanyTaxId = "0105559999999";
    private const string PrintCompanyPhone = "02-555-0100";
    private const string PrintCompanyEmail = "sales@bizcore.local";
    private readonly AccountingDbContext _context;

    public InvoicesController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var invoices = await _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Quotation)
            .Include(x => x.PaymentAllocations)
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.InvoiceId)
            .ToListAsync();

        return View(invoices);
    }

    public async Task<IActionResult> Create()
    {
        var model = new InvoiceFormViewModel
        {
            InvoiceNo = await GetNextInvoiceNumberAsync(DateTime.Today),
            Status = "Issued",
            DiscountMode = "Line"
        };

        await PopulateLookupsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var invoice = await _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Quotation)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
            .FirstOrDefaultAsync(x => x.InvoiceId == id.Value);

        if (invoice is null)
        {
            return NotFound();
        }

        if (!string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["InvoiceNotice"] = "Only draft invoices can be edited.";
            return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
        }

        var model = MapInvoiceToForm(invoice);
        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InvoiceFormViewModel model)
    {
        if (id != model.InvoiceId)
        {
            return NotFound();
        }

        var invoice = await _context.InvoiceHeaders
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
            .FirstOrDefaultAsync(x => x.InvoiceId == id);

        if (invoice is null)
        {
            return NotFound();
        }

        if (!string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["InvoiceNotice"] = "Only draft invoices can be edited.";
            return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
        }

        model.InvoiceNo = invoice.InvoiceNo;
        model.Status = "Draft";
        model.PaidAmount = invoice.PaidAmount;
        model.QuotationId = invoice.QuotationId;
        ModelState.Remove(nameof(InvoiceFormViewModel.InvoiceNo));

        if (!await ValidateAndComputeAsync(model, requireSerials: false))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        invoice.InvoiceDate = model.InvoiceDate.Date;
        invoice.CustomerId = model.CustomerId!.Value;
        invoice.SalespersonId = model.SalespersonId;
        invoice.ReferenceNo = model.ReferenceNo?.Trim();
        invoice.Remark = model.Remark?.Trim();
        invoice.Subtotal = model.Subtotal;
        invoice.DiscountAmount = model.DiscountMode == "Header" ? model.HeaderDiscountAmount : model.DiscountAmount;
        invoice.VatType = model.VatType;
        invoice.VatAmount = model.VatAmount;
        invoice.TotalAmount = model.TotalAmount;
        invoice.PaidAmount = 0m;
        invoice.BalanceAmount = model.BalanceAmount;
        invoice.Status = "Draft";
        invoice.UpdatedDate = DateTime.UtcNow;

        _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
        invoice.InvoiceDetails = model.Details.Select(MapDetailEntity).ToList();

        if (!await TrySaveAsync("Invoice number or selected serial is already in use."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceFormViewModel model)
    {
        model.InvoiceNo = await GetNextInvoiceNumberAsync(model.InvoiceDate);
        model.Status = "Issued";
        model.PaidAmount = 0m;
        ModelState.Remove(nameof(InvoiceFormViewModel.InvoiceNo));

        if (!await ValidateAndComputeAsync(model, requireSerials: true))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var trackedItemIds = model.Details.Select(x => x.ItemId!.Value).Distinct().ToList();
            var trackedSerialIds = model.Details.SelectMany(x => x.SelectedSerialIds).Distinct().ToList();

            var itemMap = await _context.Items
                .Where(x => trackedItemIds.Contains(x.ItemId))
                .ToDictionaryAsync(x => x.ItemId);

            var serialMap = trackedSerialIds.Count == 0
                ? new Dictionary<int, SerialNumber>()
                : await _context.SerialNumbers
                    .Where(x => trackedSerialIds.Contains(x.SerialId))
                    .ToDictionaryAsync(x => x.SerialId);

            var header = new InvoiceHeader
            {
                InvoiceNo = model.InvoiceNo,
                InvoiceDate = model.InvoiceDate.Date,
                CustomerId = model.CustomerId!.Value,
                SalespersonId = model.SalespersonId,
                ReferenceNo = model.ReferenceNo?.Trim(),
                Remark = model.Remark?.Trim(),
                Subtotal = model.Subtotal,
                DiscountAmount = model.DiscountMode == "Header" ? model.HeaderDiscountAmount : model.DiscountAmount,
                VatType = model.VatType,
                VatAmount = model.VatAmount,
                TotalAmount = model.TotalAmount,
                PaidAmount = 0m,
                BalanceAmount = model.BalanceAmount,
                Status = "Issued",
                CreatedDate = DateTime.UtcNow,
                InvoiceDetails = model.Details.Select(MapDetailEntity).ToList()
            };

            _context.InvoiceHeaders.Add(header);
            await _context.SaveChangesAsync();

            foreach (var detail in model.Details)
            {
                var item = itemMap[detail.ItemId!.Value];
                var isProduct = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase);
                if (isProduct && item.TrackStock)
                {
                    if (item.CurrentStock < detail.Qty)
                    {
                        throw new InvalidOperationException($"Insufficient stock for item {item.ItemCode}.");
                    }

                    item.CurrentStock -= detail.Qty;
                }
            }

            foreach (var savedDetail in header.InvoiceDetails)
            {
                foreach (var invoiceSerial in savedDetail.InvoiceSerials)
                {
                    var serial = serialMap[invoiceSerial.SerialId];
                    if (!string.Equals(serial.Status, "InStock", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Serial {serial.SerialNo} is no longer available.");
                    }

                    serial.Status = "Sold";
                    serial.CurrentCustomerId = header.CustomerId;
                    serial.InvoiceId = header.InvoiceId;
                    serial.CustomerWarrantyStartDate = savedDetail.CustomerWarrantyStartDate?.Date;
                    serial.CustomerWarrantyEndDate = savedDetail.CustomerWarrantyEndDate?.Date;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Details), new { id = header.InvoiceId });
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Invoice number or selected serial is already in use.");
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var invoice = await _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Quotation)
            .Include(x => x.PaymentAllocations)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.InvoiceId == id.Value);

        return invoice is null ? NotFound() : View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(int id)
    {
        var invoice = await _context.InvoiceHeaders
            .Include(x => x.Quotation)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
            .FirstOrDefaultAsync(x => x.InvoiceId == id);

        if (invoice is null)
        {
            return NotFound();
        }

        if (!string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["InvoiceNotice"] = "Only draft invoices can be issued.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = MapInvoiceToForm(invoice);
        if (!await ValidateAndComputeAsync(model, requireSerials: true))
        {
            TempData["InvoiceNotice"] = "Cannot issue invoice. Serial-controlled items must have selected serial count equal to quantity and complete warranty dates.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var itemIds = invoice.InvoiceDetails.Select(x => x.ItemId).Distinct().ToList();
            var serialIds = invoice.InvoiceDetails.SelectMany(x => x.InvoiceSerials).Select(x => x.SerialId).Distinct().ToList();

            var itemMap = await _context.Items
                .Where(x => itemIds.Contains(x.ItemId))
                .ToDictionaryAsync(x => x.ItemId);

            var serialMap = serialIds.Count == 0
                ? new Dictionary<int, SerialNumber>()
                : await _context.SerialNumbers
                    .Where(x => serialIds.Contains(x.SerialId))
                    .ToDictionaryAsync(x => x.SerialId);

            foreach (var detail in invoice.InvoiceDetails)
            {
                var item = itemMap[detail.ItemId];
                var isProduct = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase);
                if (isProduct && item.TrackStock)
                {
                    if (item.CurrentStock < detail.Qty)
                    {
                        throw new InvalidOperationException($"Insufficient stock for item {item.ItemCode}.");
                    }

                    item.CurrentStock -= detail.Qty;
                }

                foreach (var invoiceSerial in detail.InvoiceSerials)
                {
                    var serial = serialMap[invoiceSerial.SerialId];
                    if (!string.Equals(serial.Status, "InStock", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Serial {serial.SerialNo} is no longer available.");
                    }

                    serial.Status = "Sold";
                    serial.CurrentCustomerId = invoice.CustomerId;
                    serial.InvoiceId = invoice.InvoiceId;
                    serial.CustomerWarrantyStartDate = detail.CustomerWarrantyStartDate?.Date;
                    serial.CustomerWarrantyEndDate = detail.CustomerWarrantyEndDate?.Date;
                }
            }

            invoice.Status = "Issued";
            invoice.BalanceAmount = invoice.TotalAmount - invoice.PaidAmount;
            invoice.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["InvoiceNotice"] = "Invoice issued successfully.";
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            TempData["InvoiceNotice"] = ex.Message;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["InvoiceNotice"] = "Invoice issue failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var invoice = await _context.InvoiceHeaders
            .Include(x => x.PaymentAllocations)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.InvoiceId == id);

        if (invoice is null)
        {
            return NotFound();
        }

        if (!string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(invoice.Status, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            TempData["InvoiceNotice"] = "Only draft or issued invoices can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (invoice.PaidAmount > 0 || invoice.PaymentAllocations.Any())
        {
            TempData["InvoiceNotice"] = "Cannot cancel invoice with payment. Cancel payment first.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (string.Equals(invoice.Status, "Issued", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var detail in invoice.InvoiceDetails)
                {
                    var item = detail.Item;
                    if (item is not null && item.TrackStock)
                    {
                        item.CurrentStock += detail.Qty;
                    }

                    foreach (var invoiceSerial in detail.InvoiceSerials)
                    {
                        var serial = invoiceSerial.SerialNumber;
                        if (serial is null)
                        {
                            continue;
                        }

                        serial.Status = "InStock";
                        serial.CurrentCustomerId = null;
                        serial.InvoiceId = null;
                        serial.CustomerWarrantyStartDate = null;
                        serial.CustomerWarrantyEndDate = null;
                    }
                }
            }

            var invoiceSerials = invoice.InvoiceDetails
                .SelectMany(x => x.InvoiceSerials)
                .ToList();
            _context.InvoiceSerials.RemoveRange(invoiceSerials);

            invoice.Status = "Cancelled";
            invoice.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["InvoiceNotice"] = "Invoice cancelled successfully.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["InvoiceNotice"] = "Invoice cancellation failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var invoice = await _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Quotation)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.InvoiceId == id.Value);

        if (invoice is null)
        {
            return NotFound();
        }

        ViewData["PrintCompanyName"] = PrintCompanyName;
        ViewData["PrintCompanyAddress"] = PrintCompanyAddress;
        ViewData["PrintCompanyTaxId"] = PrintCompanyTaxId;
        ViewData["PrintCompanyPhone"] = PrintCompanyPhone;
        ViewData["PrintCompanyEmail"] = PrintCompanyEmail;
        return View(invoice);
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

    private async Task PopulateLookupsAsync(InvoiceFormViewModel model)
    {
        model.CustomerOptions = await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new SelectListItem
            {
                Value = x.CustomerId.ToString(),
                Text = $"{x.CustomerCode} - {x.CustomerName}"
            })
            .ToListAsync();

        model.SalespersonOptions = await _context.Salespersons
            .AsNoTracking()
            .OrderBy(x => x.SalespersonCode)
            .Select(x => new SelectListItem
            {
                Value = x.SalespersonId.ToString(),
                Text = $"{x.SalespersonCode} - {x.SalespersonName}"
            })
            .ToListAsync();

        model.VatTypeOptions = new[]
        {
            new SelectListItem("VAT", "VAT"),
            new SelectListItem("No VAT", "NoVAT")
        };

        model.DiscountModeOptions = new[]
        {
            new SelectListItem("Line", "Line"),
            new SelectListItem("Header", "Header")
        };

        model.CustomerLookup = await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new QuotationCustomerLookupViewModel
            {
                CustomerId = x.CustomerId,
                CustomerName = x.CustomerName,
                TaxId = x.TaxId ?? string.Empty,
                ContactName = string.Empty,
                Phone = x.PhoneNumber ?? string.Empty,
                Email = x.Email ?? string.Empty,
                BillingAddress = x.Address ?? string.Empty,
                ShippingAddress = x.Address ?? string.Empty
            })
            .ToListAsync();

        model.SalespersonLookup = await _context.Salespersons
            .AsNoTracking()
            .OrderBy(x => x.SalespersonCode)
            .Select(x => new QuotationSalespersonLookupViewModel
            {
                SalespersonId = x.SalespersonId,
                SalespersonName = x.SalespersonName,
                Phone = x.PhoneNumber ?? string.Empty,
                Email = x.Email ?? string.Empty
            })
            .ToListAsync();

        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ItemCode)
            .Select(x => new InvoiceItemLookupViewModel
            {
                ItemId = x.ItemId,
                DisplayText = $"{x.ItemCode} - {x.ItemName}",
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                PartNumber = x.PartNumber,
                ItemType = x.ItemType,
                UnitPrice = x.UnitPrice,
                TrackStock = x.TrackStock,
                IsSerialControlled = x.IsSerialControlled,
                CurrentStock = x.CurrentStock
            })
            .ToListAsync();

        var selectedSerialIds = model.Details
            .SelectMany(x => x.SelectedSerialIds)
            .Distinct()
            .ToList();

        model.SerialLookup = await _context.SerialNumbers
            .AsNoTracking()
            .Where(x => x.Status == "InStock" || selectedSerialIds.Contains(x.SerialId))
            .OrderBy(x => x.SerialNo)
            .Select(x => new InvoiceSerialLookupViewModel
            {
                SerialId = x.SerialId,
                ItemId = x.ItemId,
                SerialNo = x.SerialNo,
                DisplayText = x.SerialNo
            })
            .ToListAsync();
    }

    private static InvoiceFormViewModel MapInvoiceToForm(InvoiceHeader invoice)
    {
        var detailDiscountTotal = invoice.InvoiceDetails.Sum(x => x.DiscountAmount);
        var useHeaderDiscount = invoice.DiscountAmount > 0 && detailDiscountTotal == 0;

        return new InvoiceFormViewModel
        {
            InvoiceId = invoice.InvoiceId,
            QuotationId = invoice.QuotationId,
            InvoiceNo = invoice.InvoiceNo,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            SalespersonId = invoice.SalespersonId,
            QuotationNo = invoice.Quotation?.QuotationNumber,
            ReferenceNo = invoice.ReferenceNo,
            VatType = invoice.VatType,
            DiscountMode = useHeaderDiscount ? "Header" : "Line",
            HeaderDiscountAmount = useHeaderDiscount ? invoice.DiscountAmount : 0m,
            Remark = invoice.Remark,
            Subtotal = invoice.Subtotal,
            DiscountAmount = useHeaderDiscount ? 0m : invoice.DiscountAmount,
            VatAmount = invoice.VatAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            BalanceAmount = invoice.BalanceAmount,
            Status = invoice.Status,
            Details = invoice.InvoiceDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new InvoiceLineEditorViewModel
                {
                    InvoiceDetailId = x.InvoiceDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    ItemCode = x.Item?.ItemCode ?? string.Empty,
                    ItemName = x.Item?.ItemName ?? string.Empty,
                    PartNumber = x.Item?.PartNumber ?? string.Empty,
                    ItemType = x.Item?.ItemType ?? "Product",
                    TrackStock = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.TrackStock ?? false),
                    IsSerialControlled = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.IsSerialControlled ?? false),
                    CurrentStock = x.Item?.CurrentStock ?? 0m,
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice,
                    DiscountAmount = x.DiscountAmount,
                    LineTotal = x.LineTotal,
                    Remark = x.Remark,
                    CustomerWarrantyStartDate = x.CustomerWarrantyStartDate,
                    CustomerWarrantyEndDate = x.CustomerWarrantyEndDate,
                    SelectedSerialIds = x.InvoiceSerials.Select(s => s.SerialId).ToList()
                })
                .ToList()
        };
    }

    private async Task<bool> ValidateAndComputeAsync(InvoiceFormViewModel model, bool requireSerials)
    {
        model.Details = NormalizeDetails(model.Details);

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "Please add at least one invoice line.");
        }

        if (!ModelState.IsValid)
        {
            EnsureAtLeastOneLine(model);
            return false;
        }

        var customerExists = await _context.Customers.AnyAsync(x => x.CustomerId == model.CustomerId);
        if (!customerExists)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Selected customer was not found.");
        }

        if (model.SalespersonId.HasValue)
        {
            var salespersonExists = await _context.Salespersons.AnyAsync(x => x.SalespersonId == model.SalespersonId.Value);
            if (!salespersonExists)
            {
                ModelState.AddModelError(nameof(model.SalespersonId), "Selected salesperson was not found.");
            }
        }

        if (model.VatType is not ("VAT" or "NoVAT"))
        {
            ModelState.AddModelError(nameof(model.VatType), "VAT type must be VAT or NoVAT.");
        }

        if (model.DiscountMode is not ("Line" or "Header"))
        {
            ModelState.AddModelError(nameof(model.DiscountMode), "Discount mode must be Line or Header.");
        }

        var itemIds = model.Details.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToList();
        var itemMap = await _context.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId);

        var serialIds = model.Details.SelectMany(x => x.SelectedSerialIds).Distinct().ToList();
        var serialMap = serialIds.Count == 0
            ? new Dictionary<int, SerialNumber>()
            : await _context.SerialNumbers
                .AsNoTracking()
                .Where(x => serialIds.Contains(x.SerialId))
                .ToDictionaryAsync(x => x.SerialId);

        var duplicateSerialIds = model.Details
            .SelectMany(x => x.SelectedSerialIds)
            .GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet();

        decimal subtotal = 0m;
        decimal discount = 0m;
        var useLineDiscount = model.DiscountMode == "Line";

        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            detail.LineNumber = i + 1;

            if (!detail.ItemId.HasValue || !itemMap.TryGetValue(detail.ItemId.Value, out var item))
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "Please select a valid item.");
                continue;
            }

            detail.ItemCode = item.ItemCode;
            detail.ItemName = item.ItemName;
            detail.PartNumber = item.PartNumber;
            detail.ItemType = item.ItemType;
            detail.TrackStock = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase) && item.TrackStock;
            detail.IsSerialControlled = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase) && item.IsSerialControlled;
            detail.CurrentStock = detail.TrackStock ? item.CurrentStock : 0m;

            if (detail.UnitPrice <= 0)
            {
                detail.UnitPrice = item.UnitPrice;
            }

            if (detail.TrackStock && detail.CurrentStock < detail.Qty)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "Quantity cannot exceed current stock.");
            }

            var gross = detail.Qty * detail.UnitPrice;
            if (!useLineDiscount)
            {
                detail.DiscountAmount = 0m;
            }

            if (detail.DiscountAmount > gross)
            {
                ModelState.AddModelError($"Details[{i}].DiscountAmount", "Discount cannot exceed the line amount.");
                continue;
            }

            if (detail.IsSerialControlled)
            {
                if (detail.Qty != Math.Truncate(detail.Qty))
                {
                    ModelState.AddModelError($"Details[{i}].Qty", "Serial-controlled items must use whole-number quantity.");
                }

                if (detail.SelectedSerialIds.Count > (int)detail.Qty)
                {
                    ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Selected serial count cannot exceed quantity.");
                }

                if (requireSerials && detail.SelectedSerialIds.Count == 0)
                {
                    ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Please select serial numbers for serial-controlled items.");
                }

                if (requireSerials && detail.SelectedSerialIds.Count != (int)detail.Qty)
                {
                    ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Selected serial count must exactly match quantity.");
                }

                if (requireSerials && !detail.CustomerWarrantyStartDate.HasValue)
                {
                    ModelState.AddModelError($"Details[{i}].CustomerWarrantyStartDate", "Customer warranty start date is required for serial-controlled items.");
                }

                if (requireSerials && !detail.CustomerWarrantyEndDate.HasValue)
                {
                    ModelState.AddModelError($"Details[{i}].CustomerWarrantyEndDate", "Customer warranty end date is required for serial-controlled items.");
                }

                if (detail.CustomerWarrantyStartDate.HasValue &&
                    detail.CustomerWarrantyEndDate.HasValue &&
                    detail.CustomerWarrantyEndDate.Value.Date < detail.CustomerWarrantyStartDate.Value.Date)
                {
                    ModelState.AddModelError($"Details[{i}].CustomerWarrantyEndDate", "Customer warranty end date must be on or after the start date.");
                }

                foreach (var serialId in detail.SelectedSerialIds)
                {
                    if (duplicateSerialIds.Contains(serialId))
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "The same serial cannot be selected more than once.");
                    }

                    if (!serialMap.TryGetValue(serialId, out var serial))
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "One or more selected serials were not found.");
                        continue;
                    }

                    if (serial.ItemId != item.ItemId)
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Selected serial does not belong to the chosen item.");
                    }

                    if (!string.Equals(serial.Status, "InStock", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Only InStock serials can be invoiced.");
                    }
                }
            }
            else
            {
                detail.SelectedSerialIds = new List<int>();
                detail.CustomerWarrantyStartDate = null;
                detail.CustomerWarrantyEndDate = null;
            }

            detail.LineTotal = gross - detail.DiscountAmount;
            if (useLineDiscount)
            {
                subtotal += gross;
                discount += detail.DiscountAmount;
            }
            else
            {
                subtotal += gross;
            }
        }

        if (useLineDiscount)
        {
            model.HeaderDiscountAmount = 0m;
        }

        if (!useLineDiscount && model.HeaderDiscountAmount > subtotal)
        {
            ModelState.AddModelError(nameof(model.HeaderDiscountAmount), "Header discount cannot exceed subtotal.");
        }

        model.Subtotal = subtotal;
        model.DiscountAmount = discount;
        var appliedDiscount = useLineDiscount ? discount : model.HeaderDiscountAmount;
        var net = subtotal - appliedDiscount;
        model.VatAmount = model.VatType == "VAT" ? Math.Round(net * 0.07m, 2, MidpointRounding.AwayFromZero) : 0m;
        model.TotalAmount = net + model.VatAmount;
        model.PaidAmount = 0m;
        model.BalanceAmount = model.TotalAmount;
        EnsureAtLeastOneLine(model);
        return ModelState.IsValid;
    }

    private static void EnsureAtLeastOneLine(InvoiceFormViewModel model)
    {
        model.Details ??= new List<InvoiceLineEditorViewModel>();
    }

    private static List<InvoiceLineEditorViewModel> NormalizeDetails(IEnumerable<InvoiceLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<InvoiceLineEditorViewModel>())
            .Where(x =>
                x.ItemId.HasValue ||
                x.Qty > 0 ||
                x.UnitPrice > 0 ||
                x.DiscountAmount > 0 ||
                x.SelectedSerialIds.Count > 0 ||
                !string.IsNullOrWhiteSpace(x.Remark))
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                x.SelectedSerialIds = x.SelectedSerialIds.Distinct().ToList();
                return x;
            })
            .ToList();
    }

    private static InvoiceDetail MapDetailEntity(InvoiceLineEditorViewModel detail)
    {
        return new InvoiceDetail
        {
            LineNumber = detail.LineNumber,
            ItemId = detail.ItemId!.Value,
            Qty = detail.Qty,
            UnitPrice = detail.UnitPrice,
            DiscountAmount = detail.DiscountAmount,
            LineTotal = detail.LineTotal,
            Remark = detail.Remark?.Trim(),
            CustomerWarrantyStartDate = detail.CustomerWarrantyStartDate?.Date,
            CustomerWarrantyEndDate = detail.CustomerWarrantyEndDate?.Date,
            InvoiceSerials = detail.SelectedSerialIds
                .Select(serialId => new InvoiceSerial
                {
                    SerialId = serialId
                })
                .ToList()
        };
    }

    private Task<string> GetNextInvoiceNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.InvoiceHeaders.Select(x => x.InvoiceNo), prefix, date);
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
