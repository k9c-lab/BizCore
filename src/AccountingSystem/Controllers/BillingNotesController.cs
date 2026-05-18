using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Utilities;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BizCore.Controllers;

[Authorize]
public class BillingNotesController : CrudControllerBase
{
    private const string NumberPrefix = "BIL";
    private const decimal VatRate = 0.07m;
    private const string SummaryModeTreatmentRight = "TreatmentRight";
    private const string SummaryModeItem = "Item";

    private readonly AccountingDbContext _context;
    private readonly CompanyProfileSettings _companyProfile;

    public BillingNotesController(AccountingDbContext context, IOptions<CompanyProfileSettings> companyProfileOptions)
    {
        _context = context;
        _companyProfile = companyProfileOptions.Value;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.BillingNoteHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
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
                x.BillingNoteNo.Contains(keyword) ||
                (x.Customer != null && (
                    x.Customer.CustomerCode.Contains(keyword) ||
                    x.Customer.CustomerName.Contains(keyword))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.BillingNoteDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.BillingNoteDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var notes = await PaginatedList<BillingNoteHeader>.CreateAsync(query
            .OrderByDescending(x => x.BillingNoteDate)
            .ThenByDescending(x => x.BillingNoteId), page, pageSize);

        return View(notes);
    }

    public async Task<IActionResult> Create(int? customerId, string? summaryMode, string? search, DateTime? dateFrom, DateTime? dateTo)
    {
        var model = new BillingNoteCreateViewModel
        {
            BillingNoteNo = await GetNextBillingNoteNumberAsync(DateTime.Today),
            BillingNoteDate = DateTime.Today,
            CustomerId = customerId,
            BranchId = CurrentBranchId(),
            SummaryMode = NormalizeSummaryMode(summaryMode),
            DiscountAmount = 0m,
            Search = search,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SubmitAction = "Issue"
        };

        await PopulateCreateLookupsAsync(model);
        await LoadAvailableInvoicesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillingNoteCreateViewModel model)
    {
        var shouldIssue = string.Equals(model.SubmitAction, "Issue", StringComparison.OrdinalIgnoreCase);
        model.SummaryMode = NormalizeSummaryMode(model.SummaryMode);
        model.BillingNoteNo = await GetNextBillingNoteNumberAsync(model.BillingNoteDate);
        model.SelectedInvoiceIds = model.SelectedInvoiceIds.Distinct().ToList();
        ModelState.Remove(nameof(BillingNoteCreateViewModel.BillingNoteNo));

        if (!await ValidateCreateAsync(model))
        {
            await PopulateCreateLookupsAsync(model);
            await LoadAvailableInvoicesAsync(model);
            return View(model);
        }

        var invoices = await LoadSelectedInvoicesAsync(model.SelectedInvoiceIds);
        var lineGroups = BuildSummaryLines(invoices, model.SummaryMode);
        var billingAmounts = ComputeBillingAmounts(
            invoices.Select(x => new BillingNoteInvoiceAmountSource(
                x.BalanceAmount,
                x.TotalAmount,
                x.VatAmount,
                x.VatType)).ToList(),
            model.DiscountAmount);

        var header = new BillingNoteHeader
        {
            BillingNoteNo = model.BillingNoteNo,
            BillingNoteDate = model.BillingNoteDate.Date,
            CustomerId = model.CustomerId!.Value,
            BranchId = model.BranchId,
            SummaryMode = model.SummaryMode,
            InvoiceCount = invoices.Count,
            SubtotalAmount = billingAmounts.SubtotalAmount,
            DiscountAmount = billingAmounts.DiscountAmount,
            VatAmount = billingAmounts.VatAmount,
            TotalAmount = billingAmounts.TotalAmount,
            PaidAmount = 0m,
            BalanceAmount = billingAmounts.TotalAmount,
            Remark = model.Remark?.Trim(),
            Status = shouldIssue ? "Issued" : "Draft",
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = CurrentUserId(),
            BillingNoteInvoices = invoices
                .Select(x => new BillingNoteInvoice
                {
                    InvoiceId = x.InvoiceId,
                    BilledAmount = x.BalanceAmount
                })
                .ToList(),
            BillingNoteLines = lineGroups
        };

        _context.BillingNoteHeaders.Add(header);

        try
        {
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = header.BillingNoteId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            ModelState.AddModelError(string.Empty, "เลขที่ใบวางบิลหรือใบแจ้งหนี้ที่เลือกถูกใช้งานแล้ว");
            await PopulateCreateLookupsAsync(model);
            await LoadAvailableInvoicesAsync(model);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id, int? customerId, string? summaryMode, string? search, DateTime? dateFrom, DateTime? dateTo)
    {
        if (id is null)
        {
            return NotFound();
        }

        var note = await _context.BillingNoteHeaders
            .AsNoTracking()
            .Include(x => x.BillingNoteInvoices)
            .FirstOrDefaultAsync(x => x.BillingNoteId == id.Value);

        if (note is null || !CanAccessBranch(note.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(note.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["BillingNoteNotice"] = "แก้ไขได้เฉพาะใบวางบิลฉบับร่าง";
            return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
        }

        var model = new BillingNoteCreateViewModel
        {
            BillingNoteId = note.BillingNoteId,
            BillingNoteNo = note.BillingNoteNo,
            BillingNoteDate = note.BillingNoteDate,
            CustomerId = customerId ?? note.CustomerId,
            BranchId = note.BranchId,
            SummaryMode = NormalizeSummaryMode(summaryMode ?? note.SummaryMode),
            DiscountAmount = note.DiscountAmount,
            Remark = note.Remark,
            Search = search,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SelectedInvoiceIds = note.BillingNoteInvoices.Select(x => x.InvoiceId).ToList(),
            IsEditMode = true,
            SubmitAction = "SaveDraft"
        };

        await PopulateCreateLookupsAsync(model);
        await LoadAvailableInvoicesAsync(model);
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BillingNoteCreateViewModel model)
    {
        if (!model.BillingNoteId.HasValue)
        {
            return NotFound();
        }

        var note = await _context.BillingNoteHeaders
            .Include(x => x.BillingNoteInvoices)
            .Include(x => x.BillingNoteLines)
            .FirstOrDefaultAsync(x => x.BillingNoteId == model.BillingNoteId.Value);

        if (note is null || !CanAccessBranch(note.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(note.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["BillingNoteNotice"] = "แก้ไขได้เฉพาะใบวางบิลฉบับร่าง";
            return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
        }

        model.IsEditMode = true;
        model.SummaryMode = NormalizeSummaryMode(model.SummaryMode);
        model.BillingNoteNo = note.BillingNoteNo;
        model.SelectedInvoiceIds = model.SelectedInvoiceIds.Distinct().ToList();
        ModelState.Remove(nameof(BillingNoteCreateViewModel.BillingNoteNo));

        if (!await ValidateCreateAsync(model))
        {
            await PopulateCreateLookupsAsync(model);
            await LoadAvailableInvoicesAsync(model);
            return View("Create", model);
        }

        var shouldIssue = string.Equals(model.SubmitAction, "Issue", StringComparison.OrdinalIgnoreCase);
        var invoices = await LoadSelectedInvoicesAsync(model.SelectedInvoiceIds);
        var lineGroups = BuildSummaryLines(invoices, model.SummaryMode);
        var billingAmounts = ComputeBillingAmounts(
            invoices.Select(x => new BillingNoteInvoiceAmountSource(
                x.BalanceAmount,
                x.TotalAmount,
                x.VatAmount,
                x.VatType)).ToList(),
            model.DiscountAmount);

        note.BillingNoteDate = model.BillingNoteDate.Date;
        note.CustomerId = model.CustomerId!.Value;
        note.BranchId = model.BranchId;
        note.SummaryMode = model.SummaryMode;
        note.InvoiceCount = invoices.Count;
        note.SubtotalAmount = billingAmounts.SubtotalAmount;
        note.DiscountAmount = billingAmounts.DiscountAmount;
        note.VatAmount = billingAmounts.VatAmount;
        note.TotalAmount = billingAmounts.TotalAmount;
        note.BalanceAmount = Math.Max(note.TotalAmount - note.PaidAmount, 0m);
        note.Remark = model.Remark?.Trim();
        note.Status = shouldIssue ? "Issued" : "Draft";
        note.UpdatedDate = DateTime.UtcNow;
        note.UpdatedByUserId = CurrentUserId();

        _context.BillingNoteInvoices.RemoveRange(note.BillingNoteInvoices);
        _context.BillingNoteLines.RemoveRange(note.BillingNoteLines);
        note.BillingNoteInvoices = invoices
            .Select(x => new BillingNoteInvoice
            {
                BillingNoteId = note.BillingNoteId,
                InvoiceId = x.InvoiceId,
                BilledAmount = x.BalanceAmount
            })
            .ToList();
        note.BillingNoteLines = lineGroups;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var note = await _context.BillingNoteHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.BillingNoteLines)
                .ThenInclude(x => x.TreatmentRight)
            .Include(x => x.BillingNoteInvoices)
                .ThenInclude(x => x.InvoiceHeader!)
                    .ThenInclude(x => x.TreatmentRight)
            .FirstOrDefaultAsync(x => x.BillingNoteId == id.Value);

        if (note is null || !CanAccessBranch(note.BranchId))
        {
            return NotFound();
        }

        return View(note);
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var note = await _context.BillingNoteHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.BillingNoteLines)
                .ThenInclude(x => x.TreatmentRight)
            .Include(x => x.BillingNoteInvoices)
                .ThenInclude(x => x.InvoiceHeader!)
                    .ThenInclude(x => x.TreatmentRight)
            .FirstOrDefaultAsync(x => x.BillingNoteId == id.Value);

        if (note is null || !CanAccessBranch(note.BranchId))
        {
            return NotFound();
        }

        PopulatePrintCompanyViewData(_companyProfile);
        return View(note);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssueDraft(int id)
    {
        var note = await _context.BillingNoteHeaders
            .Include(x => x.BillingNoteInvoices)
            .Include(x => x.BillingNoteLines)
            .FirstOrDefaultAsync(x => x.BillingNoteId == id);

        if (note is null || !CanAccessBranch(note.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(note.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["BillingNoteNotice"] = "ออกใบวางบิลได้เฉพาะเอกสารฉบับร่าง";
            return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
        }

        var model = new BillingNoteCreateViewModel
        {
            BillingNoteId = note.BillingNoteId,
            BillingNoteNo = note.BillingNoteNo,
            BillingNoteDate = note.BillingNoteDate,
            CustomerId = note.CustomerId,
            BranchId = note.BranchId,
            SummaryMode = NormalizeSummaryMode(note.SummaryMode),
            DiscountAmount = note.DiscountAmount,
            Remark = note.Remark,
            SelectedInvoiceIds = note.BillingNoteInvoices.Select(x => x.InvoiceId).Distinct().ToList(),
            IsEditMode = true,
            SubmitAction = "Issue"
        };

        if (!await ValidateCreateAsync(model))
        {
            TempData["BillingNoteNotice"] = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                ?? "ไม่สามารถออกใบวางบิลได้";

            return RedirectToAction(nameof(Edit), new { id = note.BillingNoteId });
        }

        var invoices = await LoadSelectedInvoicesAsync(model.SelectedInvoiceIds);
        var lineGroups = BuildSummaryLines(invoices, model.SummaryMode);
        var billingAmounts = ComputeBillingAmounts(
            invoices.Select(x => new BillingNoteInvoiceAmountSource(
                x.BalanceAmount,
                x.TotalAmount,
                x.VatAmount,
                x.VatType)).ToList(),
            model.DiscountAmount);

        var billedAmountByInvoiceId = invoices.ToDictionary(x => x.InvoiceId, x => x.BalanceAmount);

        note.SummaryMode = model.SummaryMode;
        note.InvoiceCount = invoices.Count;
        note.SubtotalAmount = billingAmounts.SubtotalAmount;
        note.DiscountAmount = billingAmounts.DiscountAmount;
        note.VatAmount = billingAmounts.VatAmount;
        note.TotalAmount = billingAmounts.TotalAmount;
        note.PaidAmount = 0m;
        note.BalanceAmount = billingAmounts.TotalAmount;
        note.Remark = model.Remark?.Trim();
        note.Status = "Issued";

        foreach (var billingInvoice in note.BillingNoteInvoices)
        {
            if (billedAmountByInvoiceId.TryGetValue(billingInvoice.InvoiceId, out var billedAmount))
            {
                billingInvoice.BilledAmount = billedAmount;
            }
        }

        _context.BillingNoteLines.RemoveRange(note.BillingNoteLines);
        note.BillingNoteLines = lineGroups;

        await _context.SaveChangesAsync();
        TempData["BillingNoteNotice"] = "ออกใบวางบิลเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var note = await _context.BillingNoteHeaders.FirstOrDefaultAsync(x => x.BillingNoteId == id);
        if (note is null || !CanAccessBranch(note.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(note.Status, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            TempData["BillingNoteNotice"] = "ยกเลิกได้เฉพาะใบวางบิลที่ออกแล้ว";
            return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            TempData["BillingNoteNotice"] = "กรุณาระบุเหตุผลในการยกเลิกใบวางบิล";
            return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
        }

        note.Status = "Cancelled";
        note.CancelReason = cancelReason.Trim();
        note.CancelledDate = DateTime.UtcNow;
        note.CancelledByUserId = CurrentUserId();
        note.UpdatedDate = DateTime.UtcNow;
        note.UpdatedByUserId = CurrentUserId();
        await _context.SaveChangesAsync();

        TempData["BillingNoteNotice"] = "ยกเลิกใบวางบิลแล้ว";
        return RedirectToAction(nameof(Details), new { id = note.BillingNoteId });
    }

    private async Task PopulateCreateLookupsAsync(BillingNoteCreateViewModel model)
    {
        model.SummaryMode = NormalizeSummaryMode(model.SummaryMode);

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

        model.BranchName = branches.FirstOrDefault(x => x.BranchId == model.BranchId)?.BranchName ?? "No Branch";

        model.CustomerOptions = await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new SelectListItem
            {
                Value = x.CustomerId.ToString(),
                Text = $"{x.CustomerCode} - {x.CustomerName}",
                Selected = model.CustomerId.HasValue && x.CustomerId == model.CustomerId.Value
            })
            .ToListAsync();

        model.SummaryModeOptions = new[]
        {
            new SelectListItem("สรุปตามสิทธิการรักษา", SummaryModeTreatmentRight, string.Equals(model.SummaryMode, SummaryModeTreatmentRight, StringComparison.OrdinalIgnoreCase)),
            new SelectListItem("สรุปตามรายการสินค้า", SummaryModeItem, string.Equals(model.SummaryMode, SummaryModeItem, StringComparison.OrdinalIgnoreCase))
        };
    }

    private async Task LoadAvailableInvoicesAsync(BillingNoteCreateViewModel model)
    {
        model.SummaryMode = NormalizeSummaryMode(model.SummaryMode);
        model.AvailableInvoices = new List<BillingNoteInvoiceCandidateViewModel>();
        model.SummaryPreview = new List<BillingNoteSummaryPreviewViewModel>();
        model.SelectedTotalAmount = 0m;
        model.PreviewSubtotalAmount = 0m;
        model.PreviewVatAmount = 0m;
        model.PreviewTotalAmount = 0m;

        if (!model.CustomerId.HasValue)
        {
            return;
        }

        var query = _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.TreatmentRight)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Where(x =>
                x.CustomerId == model.CustomerId.Value &&
                x.Status != "Draft" &&
                x.Status != "Cancelled" &&
                x.BalanceAmount > 0 &&
                (!x.BillingNoteInvoices.Any(y => y.BillingNoteHeader != null && y.BillingNoteHeader.Status != "Cancelled") ||
                 (model.BillingNoteId.HasValue && x.BillingNoteInvoices.Any(y => y.BillingNoteId == model.BillingNoteId.Value))));

        if (!CurrentUserCanAccessAllBranches())
        {
            var branchId = CurrentBranchId();
            query = query.Where(x => x.BranchId == branchId);
        }
        else if (model.BranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == model.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var keyword = model.Search.Trim();
            query = query.Where(x =>
                x.InvoiceNo.Contains(keyword) ||
                (x.PatientFullName != null && x.PatientFullName.Contains(keyword)) ||
                (x.PatientHn != null && x.PatientHn.Contains(keyword)));
        }

        if (model.DateFrom.HasValue)
        {
            query = query.Where(x => x.InvoiceDate >= model.DateFrom.Value.Date);
        }

        if (model.DateTo.HasValue)
        {
            var endDate = model.DateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.InvoiceDate < endDate);
        }

        var invoices = await query
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.InvoiceId)
            .ToListAsync();

        model.AvailableInvoices = invoices
            .Select(x => new BillingNoteInvoiceCandidateViewModel
            {
                InvoiceId = x.InvoiceId,
                InvoiceNo = x.InvoiceNo,
                InvoiceDate = x.InvoiceDate,
                PatientFullName = x.PatientFullName ?? string.Empty,
                PatientHn = x.PatientHn ?? string.Empty,
                TreatmentRightName = x.TreatmentRight != null
                    ? $"{x.TreatmentRight.TreatmentRightCode} - {x.TreatmentRight.TreatmentRightName}"
                    : "ไม่ระบุสิทธิการรักษา",
                BalanceAmount = x.BalanceAmount,
                TotalAmount = x.TotalAmount,
                VatAmount = x.VatAmount,
                VatType = x.VatType,
                Status = x.Status,
                Selected = model.SelectedInvoiceIds.Contains(x.InvoiceId),
                ItemLines = x.InvoiceDetails
                    .OrderBy(y => y.LineNumber)
                    .Select(y => new BillingNoteInvoiceItemCandidateViewModel
                    {
                        ItemCode = y.Item?.ItemCode ?? string.Empty,
                        ItemName = y.Item?.ItemName ?? string.Empty,
                        Qty = y.Qty,
                        LineTotal = y.LineTotal
                    })
                    .ToList()
            })
            .ToList();

        var selectedInvoices = model.AvailableInvoices.Where(x => x.Selected).ToList();
        model.SelectedTotalAmount = selectedInvoices.Sum(x => x.BalanceAmount);
        model.SummaryPreview = BuildSummaryPreview(selectedInvoices, model.SummaryMode);

        var previewAmounts = ComputeBillingAmounts(
            selectedInvoices.Select(x => new BillingNoteInvoiceAmountSource(
                x.BalanceAmount,
                x.TotalAmount,
                x.VatAmount,
                x.VatType)).ToList(),
            model.DiscountAmount);

        model.PreviewSubtotalAmount = previewAmounts.SubtotalAmount;
        model.PreviewVatAmount = previewAmounts.VatAmount;
        model.PreviewTotalAmount = previewAmounts.TotalAmount;
    }

    private async Task<bool> ValidateCreateAsync(BillingNoteCreateViewModel model)
    {
        model.SummaryMode = NormalizeSummaryMode(model.SummaryMode);

        if (!CurrentUserCanAccessAllBranches())
        {
            model.BranchId = CurrentBranchId();
        }

        if (!model.CustomerId.HasValue || !await _context.Customers.AnyAsync(x => x.CustomerId == model.CustomerId.Value))
        {
            ModelState.AddModelError(nameof(model.CustomerId), "ไม่พบลูกค้าที่เลือก");
        }

        if (!CanAccessBranch(model.BranchId))
        {
            ModelState.AddModelError(nameof(model.BranchId), "คุณไม่มีสิทธิ์ใช้งานสาขานี้");
        }

        if (model.SelectedInvoiceIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.SelectedInvoiceIds), "กรุณาเลือกใบแจ้งหนี้อย่างน้อย 1 ใบ");
        }

        if (!ModelState.IsValid)
        {
            return false;
        }

        var invoices = await _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.BillingNoteInvoices)
                .ThenInclude(x => x.BillingNoteHeader)
            .Include(x => x.InvoiceDetails)
            .Where(x => model.SelectedInvoiceIds.Contains(x.InvoiceId))
            .ToListAsync();

        if (invoices.Count != model.SelectedInvoiceIds.Count)
        {
            ModelState.AddModelError(nameof(model.SelectedInvoiceIds), "มีใบแจ้งหนี้บางรายการไม่ถูกต้อง");
            return false;
        }

        var customerId = model.CustomerId ?? 0;

        if (invoices.Any(x => x.CustomerId != customerId))
        {
            ModelState.AddModelError(nameof(model.SelectedInvoiceIds), "ใบแจ้งหนี้ที่เลือกต้องเป็นลูกค้าคนเดียวกัน");
        }

        if (model.BranchId.HasValue && invoices.Any(x => x.BranchId != model.BranchId))
        {
            ModelState.AddModelError(nameof(model.SelectedInvoiceIds), "ใบแจ้งหนี้ที่เลือกต้องเป็นสาขาเดียวกัน");
        }

        if (invoices.Any(x => x.Status == "Draft" || x.Status == "Cancelled" || x.BalanceAmount <= 0))
        {
            ModelState.AddModelError(nameof(model.SelectedInvoiceIds), "เลือกได้เฉพาะใบแจ้งหนี้ที่ออกแล้วและยังมียอดค้าง");
        }

        if (invoices.Any(x => x.BillingNoteInvoices.Any(y =>
            y.BillingNoteHeader != null &&
            y.BillingNoteHeader.Status != "Cancelled" &&
            (!model.BillingNoteId.HasValue || y.BillingNoteId != model.BillingNoteId.Value))))
        {
            ModelState.AddModelError(nameof(model.SelectedInvoiceIds), "มีใบแจ้งหนี้บางรายการถูกใช้ในใบวางบิลแล้ว");
        }

        if (string.Equals(model.SummaryMode, SummaryModeItem, StringComparison.OrdinalIgnoreCase) &&
            invoices.Any(x => !x.InvoiceDetails.Any()))
        {
            ModelState.AddModelError(nameof(model.SelectedInvoiceIds), "มีใบแจ้งหนี้บางรายการไม่มีรายการสินค้า จึงไม่สามารถสรุปตามรายการสินค้าได้");
        }

        var subtotalAmount = ComputeBillingAmounts(
            invoices.Select(x => new BillingNoteInvoiceAmountSource(
                x.BalanceAmount,
                x.TotalAmount,
                x.VatAmount,
                x.VatType)).ToList(),
            0m).SubtotalAmount;

        if (model.DiscountAmount > subtotalAmount)
        {
            ModelState.AddModelError(nameof(model.DiscountAmount), "ส่วนลดทั้งใบต้องไม่มากกว่ายอดก่อน VAT");
        }

        return ModelState.IsValid;
    }

    private async Task<List<InvoiceHeader>> LoadSelectedInvoicesAsync(List<int> selectedInvoiceIds)
    {
        return await _context.InvoiceHeaders
            .Include(x => x.TreatmentRight)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Where(x => selectedInvoiceIds.Contains(x.InvoiceId))
            .OrderBy(x => x.InvoiceDate)
            .ThenBy(x => x.InvoiceId)
            .ToListAsync();
    }

    private static List<BillingNoteLine> BuildSummaryLines(IEnumerable<InvoiceHeader> invoices, string summaryMode)
    {
        var normalizedSummaryMode = NormalizeSummaryMode(summaryMode);
        if (string.Equals(normalizedSummaryMode, SummaryModeItem, StringComparison.OrdinalIgnoreCase))
        {
            return invoices
                .SelectMany(invoice => invoice.InvoiceDetails.Select(detail => new
                {
                    invoice.InvoiceId,
                    Description = BuildItemDescription(detail.Item?.ItemCode, detail.Item?.ItemName),
                    Quantity = ComputeOpenItemQuantity(invoice.BalanceAmount, invoice.TotalAmount, detail.Qty),
                    TotalAmount = ComputeOpenItemAmount(invoice.BalanceAmount, invoice.InvoiceDetails, detail)
                }))
                .Where(x => x.Quantity > 0m || x.TotalAmount > 0m)
                .GroupBy(x => x.Description)
                .OrderBy(x => x.Key)
                .Select((group, index) => new BillingNoteLine
                {
                    LineNumber = index + 1,
                    SummaryType = SummaryModeItem,
                    Description = group.Key,
                    Quantity = Math.Round(group.Sum(x => x.Quantity), 2, MidpointRounding.AwayFromZero),
                    InvoiceCount = group.Select(x => x.InvoiceId).Distinct().Count(),
                    TotalAmount = Math.Round(group.Sum(x => x.TotalAmount), 2, MidpointRounding.AwayFromZero)
                })
                .ToList();
        }

        return invoices
            .GroupBy(x => new
            {
                x.TreatmentRightId,
                Label = x.TreatmentRight != null
                    ? $"{x.TreatmentRight.TreatmentRightCode} - {x.TreatmentRight.TreatmentRightName}"
                    : "ไม่ระบุสิทธิการรักษา"
            })
            .OrderBy(x => x.Key.Label)
            .Select((group, index) => new BillingNoteLine
            {
                LineNumber = index + 1,
                SummaryType = SummaryModeTreatmentRight,
                TreatmentRightId = group.Key.TreatmentRightId,
                Description = group.Key.Label,
                Quantity = group.Count(),
                InvoiceCount = group.Count(),
                TotalAmount = group.Sum(x => x.BalanceAmount)
            })
            .ToList();
    }

    private static List<BillingNoteSummaryPreviewViewModel> BuildSummaryPreview(
        IEnumerable<BillingNoteInvoiceCandidateViewModel> invoices,
        string summaryMode)
    {
        var selectedInvoices = invoices.ToList();
        var normalizedSummaryMode = NormalizeSummaryMode(summaryMode);

        if (string.Equals(normalizedSummaryMode, SummaryModeItem, StringComparison.OrdinalIgnoreCase))
        {
            return selectedInvoices
                .SelectMany(invoice => invoice.ItemLines.Select(line => new
                {
                    invoice.InvoiceId,
                    Label = BuildItemDescription(line.ItemCode, line.ItemName),
                    Quantity = ComputeOpenItemQuantity(invoice.BalanceAmount, invoice.TotalAmount, line.Qty),
                    TotalAmount = ComputeOpenItemAmount(invoice.BalanceAmount, invoice.ItemLines, line)
                }))
                .Where(x => x.Quantity > 0m || x.TotalAmount > 0m)
                .GroupBy(x => x.Label)
                .OrderBy(x => x.Key)
                .Select(x => new BillingNoteSummaryPreviewViewModel
                {
                    Label = x.Key,
                    Quantity = Math.Round(x.Sum(y => y.Quantity), 2, MidpointRounding.AwayFromZero),
                    InvoiceCount = x.Select(y => y.InvoiceId).Distinct().Count(),
                    TotalAmount = Math.Round(x.Sum(y => y.TotalAmount), 2, MidpointRounding.AwayFromZero)
                })
                .ToList();
        }

        return selectedInvoices
            .GroupBy(x => x.TreatmentRightName)
            .OrderBy(x => x.Key)
            .Select(x => new BillingNoteSummaryPreviewViewModel
            {
                Label = x.Key,
                Quantity = x.Count(),
                InvoiceCount = x.Count(),
                TotalAmount = x.Sum(y => y.BalanceAmount)
            })
            .ToList();
    }

    private static BillingNoteComputedAmounts ComputeBillingAmounts(
        IReadOnlyCollection<BillingNoteInvoiceAmountSource> invoices,
        decimal documentDiscount)
    {
        if (invoices.Count == 0)
        {
            return new BillingNoteComputedAmounts(0m, 0m, 0m, 0m);
        }

        decimal subtotalAmount = 0m;
        decimal taxableSubtotalAmount = 0m;

        foreach (var invoice in invoices)
        {
            var openAmount = invoice.BalanceAmount < 0m ? 0m : invoice.BalanceAmount;
            if (openAmount <= 0m)
            {
                continue;
            }

            if (!VatModeHelper.IsTaxable(invoice.VatType))
            {
                subtotalAmount += openAmount;
                continue;
            }

            var invoiceTotal = invoice.TotalAmount <= 0m ? openAmount : invoice.TotalAmount;
            var ratio = invoiceTotal <= 0m ? 0m : openAmount / invoiceTotal;
            var proratedVat = Math.Round(invoice.VatAmount * ratio, 2, MidpointRounding.AwayFromZero);
            proratedVat = Math.Min(proratedVat, openAmount);
            var preVatAmount = openAmount - proratedVat;

            subtotalAmount += preVatAmount;
            taxableSubtotalAmount += preVatAmount;
        }

        subtotalAmount = Math.Round(subtotalAmount, 2, MidpointRounding.AwayFromZero);
        taxableSubtotalAmount = Math.Round(taxableSubtotalAmount, 2, MidpointRounding.AwayFromZero);

        var normalizedDiscount = Math.Min(
            Math.Max(documentDiscount, 0m),
            subtotalAmount);

        var discountOnVatBase = subtotalAmount <= 0m || taxableSubtotalAmount <= 0m
            ? 0m
            : Math.Round(normalizedDiscount * taxableSubtotalAmount / subtotalAmount, 2, MidpointRounding.AwayFromZero);

        discountOnVatBase = Math.Min(discountOnVatBase, taxableSubtotalAmount);

        var vatBaseAfterDiscount = Math.Max(taxableSubtotalAmount - discountOnVatBase, 0m);
        var vatAmount = Math.Round(vatBaseAfterDiscount * VatRate, 2, MidpointRounding.AwayFromZero);
        var totalAmount = Math.Round(subtotalAmount - normalizedDiscount + vatAmount, 2, MidpointRounding.AwayFromZero);

        return new BillingNoteComputedAmounts(subtotalAmount, normalizedDiscount, vatAmount, totalAmount);
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private Task<string> GetNextBillingNoteNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.BillingNoteHeaders.Select(x => x.BillingNoteNo), prefix, date);
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }

    private static string NormalizeSummaryMode(string? summaryMode)
    {
        return string.Equals(summaryMode, SummaryModeItem, StringComparison.OrdinalIgnoreCase)
            ? SummaryModeItem
            : SummaryModeTreatmentRight;
    }

    private static string BuildItemDescription(string? itemCode, string? itemName)
    {
        var code = itemCode?.Trim();
        var name = itemName?.Trim();

        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
        {
            return "ไม่ระบุสินค้า";
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return name!;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return code!;
        }

        return $"{code} - {name}";
    }

    private static decimal ComputeOpenItemQuantity(decimal balanceAmount, decimal totalAmount, decimal qty)
    {
        if (qty <= 0m || balanceAmount <= 0m)
        {
            return 0m;
        }

        var ratio = totalAmount > 0m
            ? Math.Min(Math.Max(balanceAmount / totalAmount, 0m), 1m)
            : 1m;

        return Math.Round(qty * ratio, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal ComputeOpenItemAmount(decimal balanceAmount, IEnumerable<InvoiceDetail> details, InvoiceDetail detail)
    {
        var subtotalAmount = details.Sum(x => x.LineTotal);
        if (balanceAmount <= 0m || detail.LineTotal <= 0m || subtotalAmount <= 0m)
        {
            return 0m;
        }

        var ratio = detail.LineTotal / subtotalAmount;
        return Math.Round(balanceAmount * ratio, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal ComputeOpenItemAmount(decimal balanceAmount, IEnumerable<BillingNoteInvoiceItemCandidateViewModel> details, BillingNoteInvoiceItemCandidateViewModel detail)
    {
        var subtotalAmount = details.Sum(x => x.LineTotal);
        if (balanceAmount <= 0m || detail.LineTotal <= 0m || subtotalAmount <= 0m)
        {
            return 0m;
        }

        var ratio = detail.LineTotal / subtotalAmount;
        return Math.Round(balanceAmount * ratio, 2, MidpointRounding.AwayFromZero);
    }

    private sealed record BillingNoteInvoiceAmountSource(decimal BalanceAmount, decimal TotalAmount, decimal VatAmount, string? VatType);
    private sealed record BillingNoteComputedAmounts(decimal SubtotalAmount, decimal DiscountAmount, decimal VatAmount, decimal TotalAmount);
}
