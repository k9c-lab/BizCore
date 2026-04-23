using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin,BranchAdmin,Warehouse")]
public class StockTransfersController : CrudControllerBase
{
    private const string NumberPrefix = "ST";
    private readonly AccountingDbContext _context;

    public StockTransfersController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.StockTransferHeaders
            .AsNoTracking()
            .Include(x => x.FromBranch)
            .Include(x => x.ToBranch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.StockTransferDetails)
                .ThenInclude(x => x.Item)
            .AsQueryable();

        if (!CurrentUserCanAccessAllBranches())
        {
            var branchId = CurrentBranchId();
            query = query.Where(x => x.FromBranchId == branchId || x.ToBranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.TransferNo.Contains(keyword) ||
                (x.Remark != null && x.Remark.Contains(keyword)) ||
                (x.FromBranch != null && (x.FromBranch.BranchCode.Contains(keyword) || x.FromBranch.BranchName.Contains(keyword))) ||
                (x.ToBranch != null && (x.ToBranch.BranchCode.Contains(keyword) || x.ToBranch.BranchName.Contains(keyword))) ||
                x.StockTransferDetails.Any(d => d.Item != null &&
                    (d.Item.ItemCode.Contains(keyword) ||
                     d.Item.ItemName.Contains(keyword) ||
                     d.Item.PartNumber.Contains(keyword))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.TransferDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.TransferDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var transfers = await PaginatedList<StockTransferHeader>.CreateAsync(query
            .OrderByDescending(x => x.TransferDate)
            .ThenByDescending(x => x.StockTransferId), page, pageSize);

        return View(transfers);
    }

    public async Task<IActionResult> Create()
    {
        var model = new StockTransferFormViewModel
        {
            TransferNo = await GetNextTransferNumberAsync(DateTime.Today),
            FromBranchId = CurrentBranchId()
        };

        EnsureMinimumRows(model);
        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockTransferFormViewModel model, string command)
    {
        model.TransferNo = await EnsureTransferNumberAsync(model.TransferNo, model.TransferDate);
        model.Status = "Draft";
        ModelState.Remove(nameof(StockTransferFormViewModel.TransferNo));
        ModelState.Remove(nameof(StockTransferFormViewModel.Status));
        var saveDraft = IsSaveDraftCommand(command);

        if (!await ValidateTransferAsync(model, requireComplete: !saveDraft))
        {
            EnsureMinimumRows(model);
            await PopulateLookupsAsync(model);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            var header = new StockTransferHeader
            {
                TransferNo = model.TransferNo.Trim(),
                TransferDate = model.TransferDate,
                FromBranchId = model.FromBranchId!.Value,
                ToBranchId = model.ToBranchId!.Value,
                Status = saveDraft ? "Draft" : "Posted",
                Remark = model.Remark?.Trim(),
                CreatedByUserId = userId,
                PostedByUserId = saveDraft ? null : userId,
                PostedDate = saveDraft ? null : now,
                CreatedDate = now,
                StockTransferDetails = model.Details.Select(MapDetailEntity).ToList()
            };

            _context.StockTransferHeaders.Add(header);
            await _context.SaveChangesAsync();
            await AddTransferSerialsAsync(header, model);
            if (!saveDraft)
            {
                await ApplyPostedTransferAsync(header);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = header.StockTransferId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Transfer number must be unique.");
            EnsureMinimumRows(model);
            await PopulateLookupsAsync(model);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var transfer = await GetTransferForEditAsync(id.Value);
        if (transfer is null || !CanAccessTransfer(transfer))
        {
            return NotFound();
        }

        if (transfer.Status != "Draft")
        {
            TempData["StockTransferNotice"] = "Only Draft stock transfers can be edited.";
            return RedirectToAction(nameof(Details), new { id = transfer.StockTransferId });
        }

        var model = BuildFormModel(transfer);
        EnsureMinimumRows(model);
        await PopulateLookupsAsync(model);
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StockTransferFormViewModel model, string command)
    {
        var transfer = await GetTransferForEditAsync(id);
        if (transfer is null || !CanAccessTransfer(transfer))
        {
            return NotFound();
        }

        if (transfer.Status != "Draft")
        {
            TempData["StockTransferNotice"] = "Only Draft stock transfers can be edited.";
            return RedirectToAction(nameof(Details), new { id = transfer.StockTransferId });
        }

        model.StockTransferId = id;
        model.TransferNo = await EnsureTransferNumberAsync(model.TransferNo, model.TransferDate);
        model.Status = transfer.Status;
        ModelState.Remove(nameof(StockTransferFormViewModel.TransferNo));
        ModelState.Remove(nameof(StockTransferFormViewModel.Status));
        var saveDraft = IsSaveDraftCommand(command);

        if (!await ValidateTransferAsync(model, requireComplete: !saveDraft))
        {
            EnsureMinimumRows(model);
            await PopulateLookupsAsync(model);
            return View("Create", model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;
            var userId = CurrentUserId();
            transfer.TransferNo = model.TransferNo.Trim();
            transfer.TransferDate = model.TransferDate;
            transfer.FromBranchId = model.FromBranchId!.Value;
            transfer.ToBranchId = model.ToBranchId!.Value;
            transfer.Status = saveDraft ? "Draft" : "Posted";
            transfer.Remark = model.Remark?.Trim();
            transfer.UpdatedByUserId = userId;
            transfer.UpdatedDate = now;
            transfer.PostedByUserId = saveDraft ? null : userId;
            transfer.PostedDate = saveDraft ? null : now;

            _context.StockTransferDetails.RemoveRange(transfer.StockTransferDetails);
            transfer.StockTransferDetails = model.Details.Select(MapDetailEntity).ToList();
            await _context.SaveChangesAsync();
            await AddTransferSerialsAsync(transfer, model);
            if (!saveDraft)
            {
                await ApplyPostedTransferAsync(transfer);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = transfer.StockTransferId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Transfer number must be unique.");
            EnsureMinimumRows(model);
            await PopulateLookupsAsync(model);
            return View("Create", model);
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var transfer = await GetTransferDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StockTransferId == id.Value);

        return transfer is null || !CanAccessTransfer(transfer) ? NotFound() : View(transfer);
    }

    [HttpGet]
    public async Task<IActionResult> AvailableSerials(int? itemId, int? fromBranchId)
    {
        if (!itemId.HasValue || !fromBranchId.HasValue)
        {
            return Json(Array.Empty<object>());
        }

        if (!CanTransferFromBranch(fromBranchId.Value))
        {
            return Forbid();
        }

        var serials = await _context.SerialNumbers
            .AsNoTracking()
            .Where(x =>
                x.ItemId == itemId.Value &&
                x.BranchId == fromBranchId.Value &&
                x.Status == "InStock" &&
                x.InvoiceId == null &&
                x.CurrentCustomerId == null)
            .OrderBy(x => x.SerialNo)
            .Select(x => new
            {
                x.SerialId,
                x.SerialNo
            })
            .ToListAsync();

        return Json(serials);
    }

    [HttpGet]
    public async Task<IActionResult> AvailableStock(int? itemId, int? fromBranchId)
    {
        if (!itemId.HasValue || !fromBranchId.HasValue)
        {
            return Json(new { qtyOnHand = 0m });
        }

        if (!CanTransferFromBranch(fromBranchId.Value))
        {
            return Forbid();
        }

        var qtyOnHand = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.ItemId == itemId.Value && x.BranchId == fromBranchId.Value)
            .Select(x => x.QtyOnHand)
            .FirstOrDefaultAsync();

        return Json(new { qtyOnHand });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(int id)
    {
        var transfer = await GetTransferDetailsQuery()
            .FirstOrDefaultAsync(x => x.StockTransferId == id);

        if (transfer is null || !CanAccessTransfer(transfer))
        {
            return NotFound();
        }

        if (transfer.Status != "Draft")
        {
            TempData["StockTransferNotice"] = "Only Draft stock transfers can be posted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = BuildFormModel(transfer);
        if (!await ValidateTransferAsync(model, requireComplete: true))
        {
            TempData["StockTransferNotice"] = GetFirstModelStateErrorMessage("Post Stock Transfer is blocked because this draft is not complete.");
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        transfer.Status = "Posted";
        transfer.PostedByUserId = CurrentUserId();
        transfer.PostedDate = now;
        transfer.UpdatedByUserId = CurrentUserId();
        transfer.UpdatedDate = now;

        await ApplyPostedTransferAsync(transfer);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["StockTransferNotice"] = "Stock transfer posted successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var transfer = await GetTransferDetailsQuery()
            .FirstOrDefaultAsync(x => x.StockTransferId == id);

        if (transfer is null || !CanAccessTransfer(transfer))
        {
            return NotFound();
        }

        var blockReason = await GetCancelBlockedReasonAsync(transfer);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["StockTransferNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        if (transfer.Status == "Posted")
        {
            await ReversePostedTransferAsync(transfer);
        }

        transfer.Status = "Cancelled";
        transfer.CancelledByUserId = CurrentUserId();
        transfer.CancelledDate = now;
        transfer.CancelReason = NormalizeCancelReason(cancelReason);
        transfer.UpdatedByUserId = CurrentUserId();
        transfer.UpdatedDate = now;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["StockTransferNotice"] = "Stock transfer cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<StockTransferHeader> GetTransferDetailsQuery()
    {
        return _context.StockTransferHeaders
            .Include(x => x.FromBranch)
            .Include(x => x.ToBranch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.PostedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.StockTransferDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.StockTransferDetails)
                .ThenInclude(x => x.StockTransferSerials)
                    .ThenInclude(x => x.SerialNumber);
    }

    private Task<StockTransferHeader?> GetTransferForEditAsync(int id)
    {
        return _context.StockTransferHeaders
            .Include(x => x.StockTransferDetails)
                .ThenInclude(x => x.StockTransferSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.StockTransferId == id);
    }

    private async Task PopulateLookupsAsync(StockTransferFormViewModel model)
    {
        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        model.CanAccessAllBranches = canAccessAllBranches;
        if (!canAccessAllBranches)
        {
            model.FromBranchId = CurrentBranchId();
        }

        var branches = await _context.Branches
            .AsNoTracking()
            .Where(x => x.IsActive || x.BranchId == model.FromBranchId || x.BranchId == model.ToBranchId)
            .OrderBy(x => x.BranchCode)
            .ToListAsync();

        var fromBranches = canAccessAllBranches
            ? branches
            : branches.Where(x => x.BranchId == model.FromBranchId).ToList();

        model.FromBranchOptions = fromBranches
            .Select(x => new SelectListItem($"{x.BranchCode} - {x.BranchName}", x.BranchId.ToString(), x.BranchId == model.FromBranchId))
            .ToList();

        model.ToBranchOptions = branches
            .Select(x => new SelectListItem($"{x.BranchCode} - {x.BranchName}", x.BranchId.ToString(), x.BranchId == model.ToBranchId))
            .ToList();

        model.FromBranchName = branches.FirstOrDefault(x => x.BranchId == model.FromBranchId)?.BranchName ?? "No Branch";
        model.ToBranchName = branches.FirstOrDefault(x => x.BranchId == model.ToBranchId)?.BranchName ?? string.Empty;

        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .Where(x => x.IsActive && x.TrackStock)
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
    }

    private async Task<bool> ValidateTransferAsync(StockTransferFormViewModel model, bool requireComplete)
    {
        model.Details = NormalizeDetails(model.Details);

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "Please add at least one transfer line.");
        }

        if (!model.FromBranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.FromBranchId), "Please select source branch.");
        }
        else if (!CanTransferFromBranch(model.FromBranchId.Value))
        {
            ModelState.AddModelError(nameof(model.FromBranchId), "You cannot transfer stock out of this branch.");
        }

        if (!model.ToBranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.ToBranchId), "Please select destination branch.");
        }

        if (model.FromBranchId.HasValue && model.ToBranchId.HasValue && model.FromBranchId.Value == model.ToBranchId.Value)
        {
            ModelState.AddModelError(nameof(model.ToBranchId), "Destination branch must be different from source branch.");
        }

        var branchIds = new[] { model.FromBranchId, model.ToBranchId }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var activeBranchCount = await _context.Branches.CountAsync(x => branchIds.Contains(x.BranchId) && x.IsActive);
        if (activeBranchCount != branchIds.Count)
        {
            ModelState.AddModelError(string.Empty, "One or more selected branches were not found or inactive.");
        }

        var itemIds = model.Details.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToList();
        var itemMap = await _context.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId);

        var serialTexts = new List<string>();
        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            detail.LineNumber = i + 1;

            if (!detail.ItemId.HasValue || !itemMap.TryGetValue(detail.ItemId.Value, out var item))
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "Please select a valid item.");
                continue;
            }

            if (!item.TrackStock)
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "Stock transfer supports stock-tracked items only.");
            }

            if (detail.Qty <= 0)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "Qty must be greater than zero.");
            }

            if (item.IsSerialControlled && detail.Qty != Math.Truncate(detail.Qty))
            {
                ModelState.AddModelError($"Details[{i}].Qty", "Serial-controlled items must be transferred in whole numbers.");
            }

            var lineSerials = ExtractSerialNumbers(detail);
            if (lineSerials.Count != lineSerials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                ModelState.AddModelError($"Details[{i}].SerialEntryText", "Duplicate serial numbers are not allowed in the same line.");
            }

            if (item.IsSerialControlled && lineSerials.Count != (int)detail.Qty)
            {
                ModelState.AddModelError($"Details[{i}].SerialEntryText", $"Serial count must exactly match transfer qty. Qty is {detail.Qty:N0}, selected serials are {lineSerials.Count:N0}.");
            }

            if (!item.IsSerialControlled && lineSerials.Count > 0)
            {
                ModelState.AddModelError($"Details[{i}].SerialEntryText", "Serial numbers can be entered only for serial-controlled items.");
            }

            serialTexts.AddRange(lineSerials);
        }

        if (serialTexts.Count != serialTexts.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            ModelState.AddModelError(string.Empty, "Duplicate serial numbers are not allowed across transfer lines.");
        }

        if (model.FromBranchId.HasValue)
        {
            await ValidateStockAvailabilityAsync(model, itemMap);
            await ValidateSerialAvailabilityAsync(model, itemMap, requireComplete);
        }

        return ModelState.IsValid;
    }

    private async Task ValidateStockAvailabilityAsync(
        StockTransferFormViewModel model,
        IReadOnlyDictionary<int, Item> itemMap)
    {
        if (!model.FromBranchId.HasValue)
        {
            return;
        }

        var grouped = model.Details
            .Where(x => x.ItemId.HasValue && itemMap.TryGetValue(x.ItemId.Value, out var item) && item.TrackStock)
            .GroupBy(x => x.ItemId!.Value)
            .Select(x => new { ItemId = x.Key, Qty = x.Sum(d => d.Qty) })
            .ToList();

        var itemIds = grouped.Select(x => x.ItemId).ToList();
        var balanceMap = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.BranchId == model.FromBranchId.Value && itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId, x => x.QtyOnHand);

        foreach (var row in grouped)
        {
            var availableQty = balanceMap.TryGetValue(row.ItemId, out var qty) ? qty : 0m;
            if (availableQty < row.Qty)
            {
                var item = itemMap[row.ItemId];
                ModelState.AddModelError(nameof(model.Details), $"Not enough stock for {item.ItemCode}. Available {availableQty:N2}, transfer {row.Qty:N2}.");
            }
        }

        var runningQtyByItem = new Dictionary<int, decimal>();
        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            if (!detail.ItemId.HasValue || !itemMap.TryGetValue(detail.ItemId.Value, out var item) || !item.TrackStock)
            {
                continue;
            }

            var itemId = detail.ItemId.Value;
            runningQtyByItem.TryGetValue(itemId, out var runningQty);
            runningQty += detail.Qty;
            runningQtyByItem[itemId] = runningQty;

            var availableQty = balanceMap.TryGetValue(itemId, out var qty) ? qty : 0m;
            if (runningQty > availableQty)
            {
                ModelState.AddModelError($"Details[{i}].Qty", $"Not enough stock in source branch. Available {availableQty:N2}, requested total {runningQty:N2}.");
            }
        }
    }

    private async Task ValidateSerialAvailabilityAsync(
        StockTransferFormViewModel model,
        IReadOnlyDictionary<int, Item> itemMap,
        bool requireComplete)
    {
        if (!model.FromBranchId.HasValue)
        {
            return;
        }

        var requested = model.Details
            .Where(x => x.ItemId.HasValue && itemMap.TryGetValue(x.ItemId.Value, out var item) && item.IsSerialControlled)
            .SelectMany(x => ExtractSerialNumbers(x).Select(serialNo => new { x.ItemId, SerialNo = serialNo }))
            .ToList();

        if (requested.Count == 0)
        {
            return;
        }

        var serialNos = requested.Select(x => x.SerialNo).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var serials = await _context.SerialNumbers
            .AsNoTracking()
            .Where(x => serialNos.Contains(x.SerialNo))
            .ToListAsync();

        foreach (var request in requested)
        {
            var serial = serials.FirstOrDefault(x => string.Equals(x.SerialNo, request.SerialNo, StringComparison.OrdinalIgnoreCase));
            if (serial is null)
            {
                ModelState.AddModelError(nameof(model.Details), $"Serial {request.SerialNo} was not found.");
                continue;
            }

            if (serial.ItemId != request.ItemId)
            {
                ModelState.AddModelError(nameof(model.Details), $"Serial {request.SerialNo} does not belong to selected item.");
            }

            if (serial.BranchId != model.FromBranchId.Value)
            {
                ModelState.AddModelError(nameof(model.Details), $"Serial {request.SerialNo} is not in the source branch.");
            }

            if (!string.Equals(serial.Status, "InStock", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Details), $"Serial {request.SerialNo} is not available for transfer.");
            }
        }
    }

    private async Task AddTransferSerialsAsync(StockTransferHeader header, StockTransferFormViewModel model)
    {
        var detailPairs = header.StockTransferDetails
            .OrderBy(x => x.LineNumber)
            .Zip(model.Details.OrderBy(x => x.LineNumber), (Entity, Model) => new { Entity, Model })
            .ToList();

        var serialNos = detailPairs
            .SelectMany(x => ExtractSerialNumbers(x.Model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (serialNos.Count == 0)
        {
            return;
        }

        var serialMap = await _context.SerialNumbers
            .Where(x => serialNos.Contains(x.SerialNo))
            .ToDictionaryAsync(x => x.SerialNo, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in detailPairs)
        {
            foreach (var serialNo in ExtractSerialNumbers(pair.Model))
            {
                if (serialMap.TryGetValue(serialNo, out var serial))
                {
                    pair.Entity.StockTransferSerials.Add(new StockTransferSerial
                    {
                        SerialId = serial.SerialId
                    });
                }
            }
        }
    }

    private async Task ApplyPostedTransferAsync(StockTransferHeader transfer)
    {
        foreach (var detail in transfer.StockTransferDetails)
        {
            detail.Item ??= await _context.Items.FirstAsync(x => x.ItemId == detail.ItemId);

            await AdjustStockBalanceAsync(transfer.FromBranchId, detail.ItemId, -detail.Qty);
            await AdjustStockBalanceAsync(transfer.ToBranchId, detail.ItemId, detail.Qty);

            if (detail.Item?.IsSerialControlled == true)
            {
                foreach (var transferSerial in detail.StockTransferSerials)
                {
                    if (transferSerial.SerialNumber is null)
                    {
                        transferSerial.SerialNumber = await _context.SerialNumbers.FirstAsync(x => x.SerialId == transferSerial.SerialId);
                    }

                    transferSerial.SerialNumber.BranchId = transfer.ToBranchId;
                    AddStockMovement(transfer, detail, transferSerial.SerialId, 1m, "Transfer");
                }
            }
            else
            {
                AddStockMovement(transfer, detail, null, detail.Qty, "Transfer");
            }
        }
    }

    private async Task ReversePostedTransferAsync(StockTransferHeader transfer)
    {
        foreach (var detail in transfer.StockTransferDetails)
        {
            detail.Item ??= await _context.Items.FirstAsync(x => x.ItemId == detail.ItemId);

            await AdjustStockBalanceAsync(transfer.ToBranchId, detail.ItemId, -detail.Qty);
            await AdjustStockBalanceAsync(transfer.FromBranchId, detail.ItemId, detail.Qty);

            if (detail.Item?.IsSerialControlled == true)
            {
                foreach (var transferSerial in detail.StockTransferSerials)
                {
                    if (transferSerial.SerialNumber is null)
                    {
                        transferSerial.SerialNumber = await _context.SerialNumbers.FirstAsync(x => x.SerialId == transferSerial.SerialId);
                    }

                    transferSerial.SerialNumber.BranchId = transfer.FromBranchId;
                    AddStockMovement(transfer, detail, transferSerial.SerialId, 1m, "TransferCancel");
                }
            }
            else
            {
                AddStockMovement(transfer, detail, null, detail.Qty, "TransferCancel");
            }
        }
    }

    private void AddStockMovement(StockTransferHeader transfer, StockTransferDetail detail, int? serialId, decimal qty, string movementType)
    {
        var isCancel = string.Equals(movementType, "TransferCancel", StringComparison.OrdinalIgnoreCase);
        _context.StockMovements.Add(new StockMovement
        {
            MovementDate = isCancel ? DateTime.Today : transfer.TransferDate,
            MovementType = movementType,
            ReferenceType = "StockTransfer",
            ReferenceId = transfer.StockTransferId,
            ItemId = detail.ItemId,
            SerialId = serialId,
            FromBranchId = isCancel ? transfer.ToBranchId : transfer.FromBranchId,
            ToBranchId = isCancel ? transfer.FromBranchId : transfer.ToBranchId,
            Qty = qty,
            Remark = transfer.TransferNo,
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        });
    }

    private async Task<string?> GetCancelBlockedReasonAsync(StockTransferHeader transfer)
    {
        if (transfer.Status == "Cancelled")
        {
            return "Cancelled stock transfers are read-only.";
        }

        if (transfer.Status == "Draft")
        {
            return null;
        }

        if (transfer.Status != "Posted")
        {
            return $"Cancel Stock Transfer is available only for Draft or Posted documents. Current status is {transfer.Status}.";
        }

        var grouped = transfer.StockTransferDetails
            .GroupBy(x => x.ItemId)
            .Select(x => new { ItemId = x.Key, Qty = x.Sum(d => d.Qty) })
            .ToList();
        var itemIds = grouped.Select(x => x.ItemId).ToList();
        var balanceMap = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.BranchId == transfer.ToBranchId && itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId, x => x.QtyOnHand);

        foreach (var row in grouped)
        {
            var availableQty = balanceMap.TryGetValue(row.ItemId, out var qty) ? qty : 0m;
            if (availableQty < row.Qty)
            {
                var itemCode = transfer.StockTransferDetails.First(x => x.ItemId == row.ItemId).Item?.ItemCode ?? row.ItemId.ToString();
                return $"Cancel is blocked because destination branch does not have enough stock for {itemCode}.";
            }
        }

        var serials = transfer.StockTransferDetails
            .SelectMany(x => x.StockTransferSerials)
            .Select(x => x.SerialNumber)
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        var unavailable = serials.FirstOrDefault(x =>
            x.BranchId != transfer.ToBranchId ||
            !string.Equals(x.Status, "InStock", StringComparison.OrdinalIgnoreCase) ||
            x.InvoiceId.HasValue ||
            x.CurrentCustomerId.HasValue);

        return unavailable is null
            ? null
            : $"Cancel is blocked because serial {unavailable.SerialNo} is no longer available in the destination branch.";
    }

    private async Task AdjustStockBalanceAsync(int branchId, int itemId, decimal qtyDelta)
    {
        if (qtyDelta == 0)
        {
            return;
        }

        var balance = await _context.StockBalances
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.ItemId == itemId);

        if (balance is null)
        {
            balance = new StockBalance
            {
                BranchId = branchId,
                ItemId = itemId,
                QtyOnHand = 0
            };
            _context.StockBalances.Add(balance);
        }

        balance.QtyOnHand += qtyDelta;
    }

    private StockTransferFormViewModel BuildFormModel(StockTransferHeader transfer)
    {
        return new StockTransferFormViewModel
        {
            StockTransferId = transfer.StockTransferId,
            TransferNo = transfer.TransferNo,
            TransferDate = transfer.TransferDate,
            FromBranchId = transfer.FromBranchId,
            ToBranchId = transfer.ToBranchId,
            Status = transfer.Status,
            Remark = transfer.Remark,
            Details = transfer.StockTransferDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new StockTransferLineEditorViewModel
                {
                    StockTransferDetailId = x.StockTransferDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    Qty = x.Qty,
                    Remark = x.Remark,
                    SerialEntryText = string.Join(Environment.NewLine, x.StockTransferSerials
                        .Select(s => s.SerialNumber?.SerialNo)
                        .Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .ToList()
        };
    }

    private static StockTransferDetail MapDetailEntity(StockTransferLineEditorViewModel detail)
    {
        return new StockTransferDetail
        {
            LineNumber = detail.LineNumber,
            ItemId = detail.ItemId!.Value,
            Qty = detail.Qty,
            Remark = detail.Remark?.Trim()
        };
    }

    private static List<StockTransferLineEditorViewModel> NormalizeDetails(IEnumerable<StockTransferLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<StockTransferLineEditorViewModel>())
            .Where(x => x.ItemId.HasValue || x.Qty > 0 || !string.IsNullOrWhiteSpace(x.SerialEntryText) || !string.IsNullOrWhiteSpace(x.Remark))
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                return x;
            })
            .ToList();
    }

    private static List<string> ExtractSerialNumbers(StockTransferLineEditorViewModel line)
    {
        return (line.SerialEntryText ?? string.Empty)
            .Split(new[] { "\r\n", "\n", "," }, StringSplitOptions.None)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private void EnsureMinimumRows(StockTransferFormViewModel model)
    {
        while (model.Details.Count < 3)
        {
            model.Details.Add(new StockTransferLineEditorViewModel { LineNumber = model.Details.Count + 1 });
        }
    }

    private bool CanAccessTransfer(StockTransferHeader transfer)
    {
        return CurrentUserCanAccessAllBranches()
            || transfer.FromBranchId == CurrentBranchId()
            || transfer.ToBranchId == CurrentBranchId();
    }

    private bool CanTransferFromBranch(int branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private static bool IsSaveDraftCommand(string? command)
    {
        return string.Equals(command, "SaveDraft", StringComparison.OrdinalIgnoreCase);
    }

    private string GetFirstModelStateErrorMessage(string fallback)
    {
        return ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? fallback;
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

    private Task<string> GetNextTransferNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.StockTransferHeaders.Select(x => x.TransferNo), prefix, date);
    }

    private async Task<string> EnsureTransferNumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextTransferNumberAsync(date)
            : existingNo.Trim();
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
