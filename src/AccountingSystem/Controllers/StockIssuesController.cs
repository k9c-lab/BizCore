using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class StockIssuesController : CrudControllerBase
{
    private const string NumberPrefix = "SI";
    private static readonly string[] IssueTypes = { "InternalUse", "Damaged", "Demo", "Adjustment", "Other" };
    private readonly AccountingDbContext _context;

    public StockIssuesController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, string? issueType, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.StockIssueHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.StockIssueDetails)
                .ThenInclude(x => x.Item)
            .AsQueryable();

        if (!CurrentUserCanAccessAllBranches())
        {
            var branchId = CurrentBranchId();
            query = query.Where(x => x.BranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.IssueNo.Contains(keyword) ||
                (x.Purpose != null && x.Purpose.Contains(keyword)) ||
                (x.Remark != null && x.Remark.Contains(keyword)) ||
                (x.Branch != null && (x.Branch.BranchCode.Contains(keyword) || x.Branch.BranchName.Contains(keyword))) ||
                x.StockIssueDetails.Any(d => d.Item != null &&
                    (d.Item.ItemCode.Contains(keyword) ||
                     d.Item.ItemName.Contains(keyword) ||
                     d.Item.PartNumber.Contains(keyword))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(issueType))
        {
            query = query.Where(x => x.IssueType == issueType);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.IssueDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.IssueDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["IssueType"] = issueType;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var issues = await PaginatedList<StockIssueHeader>.CreateAsync(query
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.StockIssueId), page, pageSize);

        return View(issues);
    }

    public async Task<IActionResult> Create()
    {
        var model = new StockIssueFormViewModel
        {
            IssueNo = await GetNextIssueNumberAsync(DateTime.Today),
            BranchId = CurrentBranchId()
        };

        EnsureMinimumRows(model);
        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockIssueFormViewModel model, string command)
    {
        model.IssueNo = await EnsureIssueNumberAsync(model.IssueNo, model.IssueDate);
        model.Status = "Draft";
        ModelState.Remove(nameof(StockIssueFormViewModel.IssueNo));
        ModelState.Remove(nameof(StockIssueFormViewModel.Status));
        var saveDraft = IsSaveDraftCommand(command);

        if (!await ValidateIssueAsync(model))
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
            var header = new StockIssueHeader
            {
                IssueNo = model.IssueNo.Trim(),
                IssueDate = model.IssueDate,
                BranchId = model.BranchId!.Value,
                IssueType = model.IssueType,
                Purpose = model.Purpose?.Trim(),
                Status = saveDraft ? "Draft" : "Posted",
                Remark = model.Remark?.Trim(),
                CreatedByUserId = userId,
                PostedByUserId = saveDraft ? null : userId,
                PostedDate = saveDraft ? null : now,
                CreatedDate = now,
                StockIssueDetails = model.Details.Select(MapDetailEntity).ToList()
            };

            _context.StockIssueHeaders.Add(header);
            await _context.SaveChangesAsync();
            await AddIssueSerialsAsync(header, model);
            if (!saveDraft)
            {
                await ApplyPostedIssueAsync(header);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = header.StockIssueId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "เลขที่ใบเบิกซ้ำ กรุณาตรวจสอบอีกครั้ง");
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

        var issue = await GetIssueForEditAsync(id.Value);
        if (issue is null || !CanAccessBranch(issue.BranchId))
        {
            return NotFound();
        }

        if (issue.Status != "Draft")
        {
            TempData["StockIssueNotice"] = "แก้ไขได้เฉพาะใบเบิกสถานะฉบับร่าง";
            return RedirectToAction(nameof(Details), new { id = issue.StockIssueId });
        }

        var model = BuildFormModel(issue);
        EnsureMinimumRows(model);
        await PopulateLookupsAsync(model);
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StockIssueFormViewModel model, string command)
    {
        var issue = await GetIssueForEditAsync(id);
        if (issue is null || !CanAccessBranch(issue.BranchId))
        {
            return NotFound();
        }

        if (issue.Status != "Draft")
        {
            TempData["StockIssueNotice"] = "แก้ไขได้เฉพาะใบเบิกสถานะฉบับร่าง";
            return RedirectToAction(nameof(Details), new { id = issue.StockIssueId });
        }

        model.StockIssueId = id;
        model.IssueNo = await EnsureIssueNumberAsync(model.IssueNo, model.IssueDate);
        model.Status = issue.Status;
        ModelState.Remove(nameof(StockIssueFormViewModel.IssueNo));
        ModelState.Remove(nameof(StockIssueFormViewModel.Status));
        var saveDraft = IsSaveDraftCommand(command);

        if (!await ValidateIssueAsync(model))
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
            issue.IssueNo = model.IssueNo.Trim();
            issue.IssueDate = model.IssueDate;
            issue.BranchId = model.BranchId!.Value;
            issue.IssueType = model.IssueType;
            issue.Purpose = model.Purpose?.Trim();
            issue.Status = saveDraft ? "Draft" : "Posted";
            issue.Remark = model.Remark?.Trim();
            issue.UpdatedByUserId = userId;
            issue.UpdatedDate = now;
            issue.PostedByUserId = saveDraft ? null : userId;
            issue.PostedDate = saveDraft ? null : now;

            _context.StockIssueDetails.RemoveRange(issue.StockIssueDetails);
            issue.StockIssueDetails = model.Details.Select(MapDetailEntity).ToList();
            await _context.SaveChangesAsync();
            await AddIssueSerialsAsync(issue, model);
            if (!saveDraft)
            {
                await ApplyPostedIssueAsync(issue);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Details), new { id = issue.StockIssueId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "เลขที่ใบเบิกซ้ำ กรุณาตรวจสอบอีกครั้ง");
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

        var issue = await GetIssueDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StockIssueId == id.Value);

        return issue is null || !CanAccessBranch(issue.BranchId) ? NotFound() : View(issue);
    }

    [HttpGet]
    public async Task<IActionResult> AvailableSerials(int? itemId, int? branchId)
    {
        if (!itemId.HasValue || !branchId.HasValue)
        {
            return Json(Array.Empty<object>());
        }

        if (!CanAccessBranch(branchId.Value))
        {
            return Forbid();
        }

        var serials = await _context.SerialNumbers
            .AsNoTracking()
            .Where(x =>
                x.ItemId == itemId.Value &&
                x.BranchId == branchId.Value &&
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
    public async Task<IActionResult> AvailableStock(int? itemId, int? branchId)
    {
        if (!itemId.HasValue || !branchId.HasValue)
        {
            return Json(new { qtyOnHand = 0m });
        }

        if (!CanAccessBranch(branchId.Value))
        {
            return Forbid();
        }

        var qtyOnHand = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.ItemId == itemId.Value && x.BranchId == branchId.Value)
            .Select(x => x.QtyOnHand)
            .FirstOrDefaultAsync();

        return Json(new { qtyOnHand });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(int id)
    {
        var issue = await GetIssueDetailsQuery()
            .FirstOrDefaultAsync(x => x.StockIssueId == id);

        if (issue is null || !CanAccessBranch(issue.BranchId))
        {
            return NotFound();
        }

        if (issue.Status != "Draft")
        {
            TempData["StockIssueNotice"] = "ลงรายการได้เฉพาะใบเบิกสถานะฉบับร่าง";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = BuildFormModel(issue);
        if (!await ValidateIssueAsync(model))
        {
            TempData["StockIssueNotice"] = GetFirstModelStateErrorMessage("ยังลงรายการใบเบิกไม่ได้ เพราะข้อมูลฉบับร่างยังไม่ครบ");
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        issue.Status = "Posted";
        issue.PostedByUserId = CurrentUserId();
        issue.PostedDate = now;
        issue.UpdatedByUserId = CurrentUserId();
        issue.UpdatedDate = now;

        await ApplyPostedIssueAsync(issue);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["StockIssueNotice"] = "ลงรายการใบเบิกสต็อกเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var issue = await GetIssueDetailsQuery()
            .FirstOrDefaultAsync(x => x.StockIssueId == id);

        if (issue is null || !CanAccessBranch(issue.BranchId))
        {
            return NotFound();
        }

        var blockReason = await GetCancelBlockedReasonAsync(issue);
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            TempData["StockIssueNotice"] = blockReason;
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        if (issue.Status == "Posted")
        {
            await ReversePostedIssueAsync(issue);
        }

        issue.Status = "Cancelled";
        issue.CancelledByUserId = CurrentUserId();
        issue.CancelledDate = now;
        issue.CancelReason = NormalizeCancelReason(cancelReason);
        issue.UpdatedByUserId = CurrentUserId();
        issue.UpdatedDate = now;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["StockIssueNotice"] = "ยกเลิกใบเบิกสต็อกเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<StockIssueHeader> GetIssueDetailsQuery()
    {
        return _context.StockIssueHeaders
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.PostedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.StockIssueDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.StockIssueDetails)
                .ThenInclude(x => x.StockIssueSerials)
                    .ThenInclude(x => x.SerialNumber);
    }

    private Task<StockIssueHeader?> GetIssueForEditAsync(int id)
    {
        return _context.StockIssueHeaders
            .Include(x => x.StockIssueDetails)
                .ThenInclude(x => x.StockIssueSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.StockIssueId == id);
    }

    private async Task PopulateLookupsAsync(StockIssueFormViewModel model)
    {
        var canAccessAllBranches = CurrentUserCanAccessAllBranches();
        model.CanAccessAllBranches = canAccessAllBranches;
        if (!canAccessAllBranches)
        {
            model.BranchId = CurrentBranchId();
        }

        var branches = await _context.Branches
            .AsNoTracking()
            .Where(x => x.IsActive || x.BranchId == model.BranchId)
            .OrderBy(x => x.BranchCode)
            .ToListAsync();

        model.BranchOptions = branches
            .Select(x => new SelectListItem($"{x.BranchCode} - {x.BranchName}", x.BranchId.ToString(), x.BranchId == model.BranchId))
            .ToList();

        model.BranchName = branches.FirstOrDefault(x => x.BranchId == model.BranchId)?.BranchName ?? "ไม่ระบุสาขา";
        model.IssueTypeOptions = IssueTypes.Select(x => new SelectListItem(FormatIssueType(x), x, x == model.IssueType)).ToList();

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

    private async Task<bool> ValidateIssueAsync(StockIssueFormViewModel model)
    {
        model.Details = NormalizeDetails(model.Details);

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "กรุณาเพิ่มอย่างน้อย 1 รายการ");
        }

        if (!model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "กรุณาเลือกสาขา");
        }
        else if (!CanAccessBranch(model.BranchId.Value))
        {
            ModelState.AddModelError(nameof(model.BranchId), "คุณไม่มีสิทธิ์เบิกสต็อกจากสาขานี้");
        }

        if (!IssueTypes.Contains(model.IssueType))
        {
            ModelState.AddModelError(nameof(model.IssueType), "กรุณาเลือกประเภทการเบิกที่ถูกต้อง");
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
                ModelState.AddModelError($"Details[{i}].ItemId", "กรุณาเลือกรายการสินค้าที่ถูกต้อง");
                continue;
            }

            if (!item.TrackStock)
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "ใบเบิกรองรับเฉพาะสินค้าที่ติดตามสต็อก");
            }

            if (detail.Qty <= 0)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "จำนวนต้องมากกว่า 0");
            }

            if (item.IsSerialControlled && detail.Qty != Math.Truncate(detail.Qty))
            {
                ModelState.AddModelError($"Details[{i}].Qty", "สินค้าที่ควบคุม Serial ต้องเบิกเป็นจำนวนเต็มเท่านั้น");
            }

            var lineSerials = ExtractSerialNumbers(detail);
            if (lineSerials.Count != lineSerials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                ModelState.AddModelError($"Details[{i}].SerialEntryText", "ห้ามมี Serial ซ้ำในบรรทัดเดียวกัน");
            }

            if (item.IsSerialControlled && lineSerials.Count != (int)detail.Qty)
            {
                ModelState.AddModelError($"Details[{i}].SerialEntryText", $"จำนวน Serial ต้องเท่ากับจำนวนเบิก โดยจำนวนคือ {detail.Qty:N0} และ Serial ที่เลือกคือ {lineSerials.Count:N0}");
            }

            if (!item.IsSerialControlled && lineSerials.Count > 0)
            {
                ModelState.AddModelError($"Details[{i}].SerialEntryText", "กรอก Serial ได้เฉพาะสินค้าที่ควบคุม Serial เท่านั้น");
            }

            serialTexts.AddRange(lineSerials);
        }

        if (serialTexts.Count != serialTexts.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            ModelState.AddModelError(string.Empty, "ห้ามมี Serial ซ้ำกันข้ามบรรทัดรายการ");
        }

        if (model.BranchId.HasValue)
        {
            await ValidateStockAvailabilityAsync(model, itemMap);
            await ValidateSerialAvailabilityAsync(model, itemMap);
        }

        return ModelState.IsValid;
    }

    private async Task ValidateStockAvailabilityAsync(StockIssueFormViewModel model, IReadOnlyDictionary<int, Item> itemMap)
    {
        if (!model.BranchId.HasValue)
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
            .Where(x => x.BranchId == model.BranchId.Value && itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId, x => x.QtyOnHand);

        foreach (var row in grouped)
        {
            var availableQty = balanceMap.TryGetValue(row.ItemId, out var qty) ? qty : 0m;
            if (availableQty < row.Qty)
            {
                var item = itemMap[row.ItemId];
                ModelState.AddModelError(nameof(model.Details), $"สต็อกของ {item.ItemCode} ไม่พอ คงเหลือ {availableQty:N2} แต่ต้องการเบิก {row.Qty:N2}");
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
                ModelState.AddModelError($"Details[{i}].Qty", $"สต็อกในสาขาไม่พอ คงเหลือ {availableQty:N2} แต่ร้องขอรวม {runningQty:N2}");
            }
        }
    }

    private async Task ValidateSerialAvailabilityAsync(StockIssueFormViewModel model, IReadOnlyDictionary<int, Item> itemMap)
    {
        if (!model.BranchId.HasValue)
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
                ModelState.AddModelError(nameof(model.Details), $"ไม่พบ Serial {request.SerialNo}");
                continue;
            }

            if (serial.ItemId != request.ItemId)
            {
                ModelState.AddModelError(nameof(model.Details), $"Serial {request.SerialNo} ไม่ตรงกับสินค้าที่เลือก");
            }

            if (serial.BranchId != model.BranchId.Value)
            {
                ModelState.AddModelError(nameof(model.Details), $"Serial {request.SerialNo} ไม่ได้อยู่ในสาขาที่เลือก");
            }

            if (!string.Equals(serial.Status, "InStock", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Details), $"Serial {request.SerialNo} ไม่พร้อมสำหรับการเบิก");
            }
        }
    }

    private async Task AddIssueSerialsAsync(StockIssueHeader header, StockIssueFormViewModel model)
    {
        var detailPairs = header.StockIssueDetails
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
                    pair.Entity.StockIssueSerials.Add(new StockIssueSerial
                    {
                        SerialId = serial.SerialId
                    });
                }
            }
        }
    }

    private async Task ApplyPostedIssueAsync(StockIssueHeader issue)
    {
        foreach (var detail in issue.StockIssueDetails)
        {
            detail.Item ??= await _context.Items.FirstAsync(x => x.ItemId == detail.ItemId);
            await AdjustStockBalanceAsync(issue.BranchId, detail.ItemId, -detail.Qty);

            if (detail.Item?.IsSerialControlled == true)
            {
                foreach (var issueSerial in detail.StockIssueSerials)
                {
                    issueSerial.SerialNumber ??= await _context.SerialNumbers.FirstAsync(x => x.SerialId == issueSerial.SerialId);
                    issueSerial.SerialNumber.Status = "Issued";
                    AddStockMovement(issue, detail, issueSerial.SerialId, 1m, "Issue");
                }
            }
            else
            {
                AddStockMovement(issue, detail, null, detail.Qty, "Issue");
            }
        }
    }

    private async Task ReversePostedIssueAsync(StockIssueHeader issue)
    {
        foreach (var detail in issue.StockIssueDetails)
        {
            detail.Item ??= await _context.Items.FirstAsync(x => x.ItemId == detail.ItemId);
            await AdjustStockBalanceAsync(issue.BranchId, detail.ItemId, detail.Qty);

            if (detail.Item?.IsSerialControlled == true)
            {
                foreach (var issueSerial in detail.StockIssueSerials)
                {
                    issueSerial.SerialNumber ??= await _context.SerialNumbers.FirstAsync(x => x.SerialId == issueSerial.SerialId);
                    issueSerial.SerialNumber.Status = "InStock";
                    AddStockMovement(issue, detail, issueSerial.SerialId, 1m, "IssueCancel");
                }
            }
            else
            {
                AddStockMovement(issue, detail, null, detail.Qty, "IssueCancel");
            }
        }
    }

    private void AddStockMovement(StockIssueHeader issue, StockIssueDetail detail, int? serialId, decimal qty, string movementType)
    {
        _context.StockMovements.Add(new StockMovement
        {
            MovementDate = string.Equals(movementType, "IssueCancel", StringComparison.OrdinalIgnoreCase) ? DateTime.Today : issue.IssueDate,
            MovementType = movementType,
            ReferenceType = "StockIssue",
            ReferenceId = issue.StockIssueId,
            ItemId = detail.ItemId,
            SerialId = serialId,
            FromBranchId = issue.BranchId,
            Qty = qty,
            Remark = issue.IssueNo,
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        });
    }

    private async Task<string?> GetCancelBlockedReasonAsync(StockIssueHeader issue)
    {
        if (issue.Status == "Cancelled")
        {
            return "ใบเบิกที่ยกเลิกแล้วจะเป็นแบบอ่านอย่างเดียว";
        }

        if (issue.Status == "Draft")
        {
            return null;
        }

        if (issue.Status != "Posted")
        {
            return $"ยกเลิกใบเบิกได้เฉพาะสถานะฉบับร่างหรือลงรายการแล้ว สถานะปัจจุบันคือ {issue.Status}";
        }

        var unavailable = issue.StockIssueDetails
            .SelectMany(x => x.StockIssueSerials)
            .Select(x => x.SerialNumber)
            .Where(x => x is not null)
            .Select(x => x!)
            .FirstOrDefault(x =>
                x.BranchId != issue.BranchId ||
                !string.Equals(x.Status, "Issued", StringComparison.OrdinalIgnoreCase));

        return unavailable is null
            ? null
            : $"ยังยกเลิกไม่ได้ เพราะ Serial {unavailable.SerialNo} ไม่ได้อยู่ในสถานะเบิกของเอกสารนี้แล้ว";
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

    private StockIssueFormViewModel BuildFormModel(StockIssueHeader issue)
    {
        return new StockIssueFormViewModel
        {
            StockIssueId = issue.StockIssueId,
            IssueNo = issue.IssueNo,
            IssueDate = issue.IssueDate,
            BranchId = issue.BranchId,
            IssueType = issue.IssueType,
            Purpose = issue.Purpose,
            Status = issue.Status,
            Remark = issue.Remark,
            Details = issue.StockIssueDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new StockIssueLineEditorViewModel
                {
                    StockIssueDetailId = x.StockIssueDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    Qty = x.Qty,
                    Remark = x.Remark,
                    SerialEntryText = string.Join(Environment.NewLine, x.StockIssueSerials
                        .Select(s => s.SerialNumber?.SerialNo)
                        .Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .ToList()
        };
    }

    private static StockIssueDetail MapDetailEntity(StockIssueLineEditorViewModel detail)
    {
        return new StockIssueDetail
        {
            LineNumber = detail.LineNumber,
            ItemId = detail.ItemId!.Value,
            Qty = detail.Qty,
            Remark = detail.Remark?.Trim()
        };
    }

    private static List<StockIssueLineEditorViewModel> NormalizeDetails(IEnumerable<StockIssueLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<StockIssueLineEditorViewModel>())
            .Where(x => x.ItemId.HasValue || x.Qty > 0 || !string.IsNullOrWhiteSpace(x.SerialEntryText) || !string.IsNullOrWhiteSpace(x.Remark))
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                return x;
            })
            .ToList();
    }

    private static List<string> ExtractSerialNumbers(StockIssueLineEditorViewModel line)
    {
        return (line.SerialEntryText ?? string.Empty)
            .Split(new[] { "\r\n", "\n", "," }, StringSplitOptions.None)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private void EnsureMinimumRows(StockIssueFormViewModel model)
    {
        while (model.Details.Count < 3)
        {
            model.Details.Add(new StockIssueLineEditorViewModel { LineNumber = model.Details.Count + 1 });
        }
    }

    private bool CanAccessBranch(int branchId)
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

    private static string FormatIssueType(string issueType)
    {
        return issueType switch
        {
            "InternalUse" => "ใช้ภายใน",
            "Damaged" => "ชำรุด",
            "Demo" => "สาธิต",
            "Adjustment" => "ปรับปรุง",
            "Other" => "อื่น ๆ",
            _ => issueType
        };
    }

    private Task<string> GetNextIssueNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.StockIssueHeaders.Select(x => x.IssueNo), prefix, date);
    }

    private async Task<string> EnsureIssueNumberAsync(string? existingNo, DateTime date)
    {
        return string.IsNullOrWhiteSpace(existingNo)
            ? await GetNextIssueNumberAsync(date)
            : existingNo.Trim();
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
