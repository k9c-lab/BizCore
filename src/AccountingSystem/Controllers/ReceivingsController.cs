using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public class ReceivingsController : CrudControllerBase
{
    private const string NumberPrefix = "GR";
    private readonly AccountingDbContext _context;

    public ReceivingsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var receivings = await _context.ReceivingHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrderHeader)
            .OrderByDescending(x => x.ReceiveDate)
            .ThenByDescending(x => x.ReceivingId)
            .ToListAsync();

        return View(receivings);
    }

    public async Task<IActionResult> Create()
    {
        var model = new ReceivingFormViewModel
        {
            ReceivingNo = await GetNextReceivingNumberAsync(DateTime.Today)
        };

        await PopulateLookupsAsync(model);
        if (model.PurchaseOrderLookup.Count > 0)
        {
            PopulateDetailsFromLookup(model, model.PurchaseOrderLookup[0].PurchaseOrderId);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceivingFormViewModel model)
    {
        model.ReceivingNo = await EnsureReceivingNumberAsync(model.ReceivingNo, model.ReceiveDate);
        ModelState.Remove(nameof(ReceivingFormViewModel.ReceivingNo));

        await PopulateLookupsAsync(model);
        if (!await ValidateReceivingAsync(model))
        {
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var header = new ReceivingHeader
            {
                ReceivingNo = model.ReceivingNo,
                ReceiveDate = model.ReceiveDate,
                SupplierId = model.SupplierId!.Value,
                PurchaseOrderId = model.PurchaseOrderId!.Value,
                DeliveryNoteNo = model.DeliveryNoteNo?.Trim(),
                Remark = model.Remark?.Trim(),
                Status = "Posted",
                CreatedDate = DateTime.UtcNow
            };

            _context.ReceivingHeaders.Add(header);
            await _context.SaveChangesAsync();

            var po = await _context.PurchaseOrderHeaders
                .Include(x => x.PurchaseOrderDetails)
                .FirstAsync(x => x.PurchaseOrderId == model.PurchaseOrderId.Value);

            var itemIds = model.Details.Where(x => x.QtyReceivedInput > 0).Select(x => x.ItemId).Distinct().ToList();
            var itemMap = await _context.Items.Where(x => itemIds.Contains(x.ItemId)).ToDictionaryAsync(x => x.ItemId);

            foreach (var line in model.Details.Where(x => x.QtyReceivedInput > 0).OrderBy(x => x.LineNumber))
            {
                var receivingDetail = new ReceivingDetail
                {
                    ReceivingId = header.ReceivingId,
                    PurchaseOrderDetailId = line.PurchaseOrderDetailId,
                    ItemId = line.ItemId,
                    LineNumber = line.LineNumber,
                    QtyReceived = line.QtyReceivedInput,
                    Remark = line.Remark?.Trim()
                };

                _context.ReceivingDetails.Add(receivingDetail);
                await _context.SaveChangesAsync();

                var poDetail = po.PurchaseOrderDetails.First(x => x.PurchaseOrderDetailId == line.PurchaseOrderDetailId);
                poDetail.ReceivedQty += line.QtyReceivedInput;

                var item = itemMap[line.ItemId];
                if (item.TrackStock)
                {
                    item.CurrentStock += line.QtyReceivedInput;
                }

                if (item.IsSerialControlled)
                {
                    foreach (var serialNo in ExtractSerialNumbers(line))
                    {
                        _context.ReceivingSerials.Add(new ReceivingSerial
                        {
                            ReceivingDetailId = receivingDetail.ReceivingDetailId,
                            ItemId = line.ItemId,
                            SerialNo = serialNo,
                            CreatedDate = DateTime.UtcNow
                        });

                        _context.SerialNumbers.Add(new SerialNumber
                        {
                            ItemId = line.ItemId,
                            SerialNo = serialNo,
                            Status = "InStock",
                            SupplierId = header.SupplierId,
                            CurrentCustomerId = null,
                            InvoiceId = null,
                            SupplierWarrantyStartDate = line.SupplierWarrantyStartDate?.Date,
                            SupplierWarrantyEndDate = line.SupplierWarrantyEndDate?.Date,
                            CustomerWarrantyStartDate = null,
                            CustomerWarrantyEndDate = null,
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }
            }

            po.Status = ComputePOStatus(po);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = header.ReceivingId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Receiving number or serial number must be unique.");
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var receiving = await _context.ReceivingHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrderHeader)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.ReceivingDetails)
                .ThenInclude(x => x.ReceivingSerials)
            .FirstOrDefaultAsync(x => x.ReceivingId == id.Value);

        return receiving is null ? NotFound() : View(receiving);
    }

    private async Task PopulateLookupsAsync(ReceivingFormViewModel model)
    {
        var purchaseOrders = await _context.PurchaseOrderHeaders
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrderDetails)
                .ThenInclude(x => x.Item)
            .Where(x => x.Status != "Cancelled")
            .ToListAsync();

        model.PurchaseOrderLookup = purchaseOrders
            .Select(x => new ReceivingPoLookupViewModel
            {
                PurchaseOrderId = x.PurchaseOrderId,
                PONo = x.PONo,
                SupplierId = x.SupplierId,
                SupplierName = x.Supplier?.SupplierName ?? string.Empty,
                VatType = x.VatType,
                Subtotal = x.Subtotal,
                VatAmount = x.VatAmount,
                TotalAmount = x.TotalAmount,
                Lines = x.PurchaseOrderDetails
                    .Where(d => d.Qty > d.ReceivedQty)
                    .OrderBy(d => d.LineNumber)
                    .Select(d => new ReceivingPoLookupLineViewModel
                    {
                        PurchaseOrderDetailId = d.PurchaseOrderDetailId,
                        ItemId = d.ItemId,
                        LineNumber = d.LineNumber,
                        ItemCode = d.Item?.ItemCode ?? string.Empty,
                        ItemName = d.Item?.ItemName ?? string.Empty,
                        IsSerialControlled = d.Item?.IsSerialControlled ?? false,
                        TrackStock = d.Item?.TrackStock ?? false,
                        OrderedQty = d.Qty,
                        ReceivedQty = d.ReceivedQty,
                        RemainingQty = d.Qty - d.ReceivedQty
                    })
                    .ToList()
            })
            .Where(x => x.Lines.Count > 0)
            .OrderByDescending(x => x.PurchaseOrderId)
            .ToList();

        model.PurchaseOrderOptions = model.PurchaseOrderLookup
            .Select(x => new SelectListItem($"{x.PONo} - {x.SupplierName}", x.PurchaseOrderId.ToString()))
            .ToList();
    }

    private static void PopulateDetailsFromLookup(ReceivingFormViewModel model, int purchaseOrderId)
    {
        var selected = model.PurchaseOrderLookup.FirstOrDefault(x => x.PurchaseOrderId == purchaseOrderId);
        if (selected is null)
        {
            model.Details.Clear();
            return;
        }

        model.PurchaseOrderId = selected.PurchaseOrderId;
        model.SupplierId = selected.SupplierId;
        model.Details = selected.Lines
            .Select(x => new ReceivingLineEditorViewModel
            {
                PurchaseOrderDetailId = x.PurchaseOrderDetailId,
                ItemId = x.ItemId,
                LineNumber = x.LineNumber,
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                IsSerialControlled = x.IsSerialControlled,
                TrackStock = x.TrackStock,
                OrderedQty = x.OrderedQty,
                ReceivedQty = x.ReceivedQty,
                RemainingQty = x.RemainingQty,
                QtyReceivedInput = 0,
                SerialEntryText = string.Empty,
                Serials = x.IsSerialControlled ? new List<ReceivingSerialEditorViewModel> { new() } : new List<ReceivingSerialEditorViewModel>()
            })
            .ToList();
    }

    private async Task<bool> ValidateReceivingAsync(ReceivingFormViewModel model)
    {
        if (!model.PurchaseOrderId.HasValue)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Please select a purchase order.");
            return false;
        }

        var postedLines = NormalizeReceivingDetails(model.Details);
        if (postedLines.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "Enter quantity for at least one receiving line.");
            return false;
        }

        var selectedLookup = model.PurchaseOrderLookup.FirstOrDefault(x => x.PurchaseOrderId == model.PurchaseOrderId.Value);
        if (selectedLookup is null)
        {
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Selected PO is not available for receiving.");
            return false;
        }

        model.SupplierId = selectedLookup.SupplierId;
        var supplierExists = await _context.Suppliers.AnyAsync(x => x.SupplierId == model.SupplierId.Value);
        if (!supplierExists)
        {
            ModelState.AddModelError(nameof(model.SupplierId), "Selected supplier was not found.");
        }

        var requestLines = model.Details.Where(x => x.QtyReceivedInput > 0).ToList();
        for (var i = 0; i < requestLines.Count; i++)
        {
            var line = requestLines[i];
            var lookupLine = selectedLookup.Lines.FirstOrDefault(x => x.PurchaseOrderDetailId == line.PurchaseOrderDetailId);
            if (lookupLine is null)
            {
                ModelState.AddModelError(nameof(model.Details), "One or more receiving lines are not valid for the selected PO.");
                continue;
            }

            if (line.QtyReceivedInput > lookupLine.RemainingQty)
            {
                ModelState.AddModelError($"Details[{i}].QtyReceivedInput", "Qty received cannot exceed remaining PO quantity.");
            }

            if (lookupLine.IsSerialControlled)
            {
                var serials = ExtractSerialNumbers(line);

                if (line.QtyReceivedInput != Math.Truncate(line.QtyReceivedInput))
                {
                    ModelState.AddModelError($"Details[{i}].QtyReceivedInput", "Serial-controlled items must be received in whole numbers.");
                }

                if (serials.Count == 0)
                {
                    ModelState.AddModelError($"Details[{i}].SerialEntryText", "Serial numbers are required for serial-controlled items.");
                }

                if (serials.Count != (int)line.QtyReceivedInput)
                {
                    ModelState.AddModelError($"Details[{i}].SerialEntryText", "Serial count must exactly match qty received for serial-controlled items.");
                }

                if (serials.Count != serials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                {
                    ModelState.AddModelError($"Details[{i}].SerialEntryText", "Duplicate serial numbers are not allowed in the same receiving line.");
                }

                if (!line.SupplierWarrantyStartDate.HasValue)
                {
                    ModelState.AddModelError($"Details[{i}].SupplierWarrantyStartDate", "Supplier warranty start date is required for serial-controlled items.");
                }

                if (!line.SupplierWarrantyEndDate.HasValue)
                {
                    ModelState.AddModelError($"Details[{i}].SupplierWarrantyEndDate", "Supplier warranty end date is required for serial-controlled items.");
                }

                if (line.SupplierWarrantyStartDate.HasValue &&
                    line.SupplierWarrantyEndDate.HasValue &&
                    line.SupplierWarrantyEndDate.Value.Date < line.SupplierWarrantyStartDate.Value.Date)
                {
                    ModelState.AddModelError($"Details[{i}].SupplierWarrantyEndDate", "Supplier warranty end date must be on or after the supplier warranty start date.");
                }
            }
        }

        var serialNos = requestLines
            .SelectMany(ExtractSerialNumbers)
            .ToList();

        if (serialNos.Count != serialNos.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            ModelState.AddModelError(string.Empty, "Duplicate serial numbers are not allowed.");
        }

        if (serialNos.Count > 0)
        {
            var existingSerials = await _context.SerialNumbers
                .AsNoTracking()
                .AnyAsync(x => serialNos.Contains(x.SerialNo));

            if (existingSerials)
            {
                ModelState.AddModelError(string.Empty, "One or more serial numbers already exist in stock.");
            }
        }

        return ModelState.IsValid;
    }

    private static List<string> ExtractSerialNumbers(ReceivingLineEditorViewModel line)
    {
        if (!string.IsNullOrWhiteSpace(line.SerialEntryText))
        {
            return line.SerialEntryText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        return (line.Serials ?? new List<ReceivingSerialEditorViewModel>())
            .Where(x => !string.IsNullOrWhiteSpace(x.SerialNo))
            .Select(x => x.SerialNo.Trim())
            .ToList();
    }

    private static List<ReceivingLineEditorViewModel> NormalizeReceivingDetails(IEnumerable<ReceivingLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<ReceivingLineEditorViewModel>())
            .Where(x => x.QtyReceivedInput > 0)
            .ToList();
    }

    private static string ComputePOStatus(PurchaseOrderHeader po)
    {
        if (po.Status == "Cancelled")
        {
            return po.Status;
        }

        var allReceived = po.PurchaseOrderDetails.All(x => x.ReceivedQty >= x.Qty);
        if (allReceived)
        {
            return "FullyReceived";
        }

        var anyReceived = po.PurchaseOrderDetails.Any(x => x.ReceivedQty > 0);
        return anyReceived ? "PartiallyReceived" : "Approved";
    }

    private Task<string> GetNextReceivingNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.ReceivingHeaders.Select(x => x.ReceivingNo), prefix, date);
    }

    private async Task<string> EnsureReceivingNumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextReceivingNumberAsync(date)
            : existingNo.Trim();
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
