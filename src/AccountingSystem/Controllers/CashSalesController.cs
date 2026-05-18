using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Services;
using BizCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BizCore.Controllers;

[Authorize]
public class CashSalesController : CrudControllerBase
{
    private const string NumberPrefix = "CSH";
    private readonly AccountingDbContext _context;
    private readonly CompanyProfileSettings _companyProfile;
    private readonly ISystemSettingService _systemSettingService;

    public CashSalesController(
        AccountingDbContext context,
        IOptions<CompanyProfileSettings> companyProfileOptions,
        ISystemSettingService systemSettingService)
    {
        _context = context;
        _companyProfile = companyProfileOptions.Value;
        _systemSettingService = systemSettingService;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.CashSaleHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
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
                x.CashSaleNo.Contains(keyword) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(keyword)) ||
                (x.Customer != null && (
                    x.Customer.CustomerCode.Contains(keyword) ||
                    x.Customer.CustomerName.Contains(keyword) ||
                    (x.Customer.TaxId != null && x.Customer.TaxId.Contains(keyword)))) ||
                (x.Salesperson != null && x.Salesperson.SalespersonName.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CashSaleDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.CashSaleDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var cashSales = await PaginatedList<CashSaleHeader>.CreateAsync(query
            .OrderByDescending(x => x.CashSaleDate)
            .ThenByDescending(x => x.CashSaleId), page, pageSize);

        return View(cashSales);
    }

    public async Task<IActionResult> Create()
    {
        var model = new InvoiceFormViewModel
        {
            InvoiceNo = await GetNextCashSaleNumberAsync(DateTime.Today),
            InvoiceDate = DateTime.Today,
            Status = "Draft",
            DiscountMode = "Line",
            VatType = VatModeHelper.VatExclusive,
            BranchId = CurrentBranchId(),
            ShowPatientInfo = true
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

        var cashSale = await _context.CashSaleHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.CashSaleSerials)
            .FirstOrDefaultAsync(x => x.CashSaleId == id.Value);

        if (cashSale is null || !CanAccessBranch(cashSale.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(cashSale.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["CashSaleNotice"] = "Only draft cash sales can be edited.";
            return RedirectToAction(nameof(Details), new { id = cashSale.CashSaleId });
        }

        var model = MapCashSaleToForm(cashSale);
        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceFormViewModel model, string? submitAction)
    {
        model.Details = NormalizeDetails(model.Details);
        model.Status = "Draft";
        model.ShowPatientInfo = true;
        model.QuotationId = null;
        model.QuotationNo = null;
        ModelState.Remove(nameof(InvoiceFormViewModel.InvoiceNo));
        model.InvoiceNo = await GetNextCashSaleNumberAsync(model.InvoiceDate);

        var issueCashSale = string.Equals(submitAction, "Issue", StringComparison.OrdinalIgnoreCase);
        if (!await ValidateAndComputeAsync(model, requireSerials: issueCashSale, requireAvailableStock: issueCashSale))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var header = new CashSaleHeader
            {
                CashSaleNo = model.InvoiceNo,
                CashSaleDate = model.InvoiceDate.Date,
                CustomerId = model.CustomerId!.Value,
                SalespersonId = model.SalespersonId,
                BranchId = model.BranchId,
                PriceLevelId = model.ShowPriceLevelSelector ? model.PriceLevelId : null,
                ReferenceNo = model.ReferenceNo?.Trim(),
                PatientFullName = model.PatientFullName?.Trim(),
                PatientAge = model.PatientAge,
                PatientGender = model.PatientGender?.Trim(),
                PatientHn = model.PatientHn?.Trim(),
                TreatmentRightId = model.TreatmentRightId,
                PatientWard = model.PatientWard?.Trim(),
                ReferringDoctorId = model.ReferringDoctorId,
                Remark = model.Remark?.Trim(),
                Subtotal = model.Subtotal,
                DiscountAmount = model.DiscountMode == "Header" ? model.HeaderDiscountAmount : model.DiscountAmount,
                VatType = model.VatType,
                VatAmount = model.VatAmount,
                TotalAmount = model.TotalAmount,
                Status = issueCashSale ? "Issued" : "Draft",
                CreatedByUserId = CurrentUserId(),
                CreatedDate = DateTime.UtcNow,
                IssuedByUserId = issueCashSale ? CurrentUserId() : null,
                IssuedDate = issueCashSale ? DateTime.UtcNow : null,
                CashSaleDetails = model.Details.Select(MapDetailEntity).ToList()
            };

            _context.CashSaleHeaders.Add(header);
            await _context.SaveChangesAsync();

            if (issueCashSale)
            {
                await PostStockAsync(header);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CashSaleNotice"] = issueCashSale
                ? "Cash sale issued successfully."
                : "Cash sale draft saved successfully.";
            return RedirectToAction(nameof(Details), new { id = header.CashSaleId });
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            TempData["CashSaleNotice"] = ex.Message;
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Cash sale number or selected serial is already in use.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["CashSaleNotice"] = "Cash sale save failed. No changes were saved.";
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InvoiceFormViewModel model, string? submitAction)
    {
        if (id != model.InvoiceId)
        {
            return NotFound();
        }

        var cashSale = await _context.CashSaleHeaders
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.CashSaleSerials)
            .FirstOrDefaultAsync(x => x.CashSaleId == id);

        if (cashSale is null || !CanAccessBranch(cashSale.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(cashSale.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["CashSaleNotice"] = "Only draft cash sales can be edited.";
            return RedirectToAction(nameof(Details), new { id = cashSale.CashSaleId });
        }

        model.Details = NormalizeDetails(model.Details);
        model.InvoiceNo = cashSale.CashSaleNo;
        model.InvoiceDate = model.InvoiceDate.Date;
        model.Status = "Draft";
        model.ShowPatientInfo = true;
        model.QuotationId = null;
        model.QuotationNo = null;
        model.AmountDueThisInvoice = null;
        ModelState.Remove(nameof(InvoiceFormViewModel.InvoiceNo));

        var issueCashSale = string.Equals(submitAction, "Issue", StringComparison.OrdinalIgnoreCase);
        if (!await ValidateAndComputeAsync(model, requireSerials: issueCashSale, requireAvailableStock: issueCashSale))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        cashSale.CashSaleDate = model.InvoiceDate.Date;
        cashSale.CustomerId = model.CustomerId!.Value;
        cashSale.SalespersonId = model.SalespersonId;
        cashSale.BranchId = model.BranchId;
        cashSale.PriceLevelId = model.ShowPriceLevelSelector ? model.PriceLevelId : null;
        cashSale.ReferenceNo = model.ReferenceNo?.Trim();
        cashSale.PatientFullName = model.PatientFullName?.Trim();
        cashSale.PatientAge = model.PatientAge;
        cashSale.PatientGender = model.PatientGender?.Trim();
        cashSale.PatientHn = model.PatientHn?.Trim();
        cashSale.TreatmentRightId = model.TreatmentRightId;
        cashSale.PatientWard = model.PatientWard?.Trim();
        cashSale.ReferringDoctorId = model.ReferringDoctorId;
        cashSale.Remark = model.Remark?.Trim();
        cashSale.Subtotal = model.Subtotal;
        cashSale.DiscountAmount = model.DiscountMode == "Header" ? model.HeaderDiscountAmount : model.DiscountAmount;
        cashSale.VatType = model.VatType;
        cashSale.VatAmount = model.VatAmount;
        cashSale.TotalAmount = model.TotalAmount;
        cashSale.Status = issueCashSale ? "Issued" : "Draft";
        cashSale.UpdatedDate = DateTime.UtcNow;
        cashSale.UpdatedByUserId = CurrentUserId();
        cashSale.IssuedByUserId = issueCashSale ? CurrentUserId() : null;
        cashSale.IssuedDate = issueCashSale ? DateTime.UtcNow : null;

        _context.CashSaleDetails.RemoveRange(cashSale.CashSaleDetails);
        cashSale.CashSaleDetails = model.Details.Select(MapDetailEntity).ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.SaveChangesAsync();

            if (issueCashSale)
            {
                await PostStockAsync(cashSale);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            TempData["CashSaleNotice"] = issueCashSale
                ? "Cash sale issued successfully."
                : "Cash sale draft saved successfully.";
            return RedirectToAction(nameof(Details), new { id = cashSale.CashSaleId });
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            TempData["CashSaleNotice"] = ex.Message;
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Cash sale number or selected serial is already in use.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["CashSaleNotice"] = "Cash sale save failed. No changes were saved.";
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

        var cashSale = await _context.CashSaleHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Branch)
            .Include(x => x.TreatmentRight)
            .Include(x => x.ReferringDoctor)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.CashSaleSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.CashSaleId == id.Value);

        if (cashSale is null || !CanAccessBranch(cashSale.BranchId))
        {
            return NotFound();
        }

        return View(cashSale);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(int id)
    {
        var cashSale = await _context.CashSaleHeaders
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.CashSaleSerials)
            .FirstOrDefaultAsync(x => x.CashSaleId == id);

        if (cashSale is null || !CanAccessBranch(cashSale.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(cashSale.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["CashSaleNotice"] = "Only draft cash sales can be issued.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = MapCashSaleToForm(cashSale);
        if (!await ValidateAndComputeAsync(model, requireSerials: true, requireAvailableStock: true))
        {
            TempData["CashSaleNotice"] = BuildIssueValidationNotice();
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await PostStockAsync(cashSale);
            cashSale.Status = "Issued";
            cashSale.UpdatedDate = DateTime.UtcNow;
            cashSale.UpdatedByUserId = CurrentUserId();
            cashSale.IssuedByUserId = CurrentUserId();
            cashSale.IssuedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CashSaleNotice"] = "Cash sale issued successfully.";
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            TempData["CashSaleNotice"] = ex.Message;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["CashSaleNotice"] = "Cash sale issue failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var cashSale = await _context.CashSaleHeaders
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.CashSaleSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.CashSaleId == id);

        if (cashSale is null || !CanAccessBranch(cashSale.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(cashSale.Status, "Draft", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(cashSale.Status, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            TempData["CashSaleNotice"] = "Only draft or issued cash sales can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            TempData["CashSaleNotice"] = "Please provide a cancellation reason.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (string.Equals(cashSale.Status, "Issued", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var detail in cashSale.CashSaleDetails)
                {
                    var item = detail.Item;
                    if (item is not null &&
                        string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase) &&
                        item.TrackStock)
                    {
                        await AdjustStockBalanceAsync(cashSale.BranchId, detail.ItemId, detail.Qty);
                        _context.StockMovements.Add(new StockMovement
                        {
                            MovementDate = DateTime.Today,
                            MovementType = "CashSaleCancel",
                            ReferenceType = "CashSale",
                            ReferenceId = cashSale.CashSaleId,
                            ItemId = detail.ItemId,
                            ToBranchId = cashSale.BranchId,
                            Qty = detail.Qty,
                            Remark = cashSale.CashSaleNo,
                            CreatedByUserId = CurrentUserId(),
                            CreatedDate = DateTime.UtcNow
                        });
                    }

                    foreach (var cashSaleSerial in detail.CashSaleSerials)
                    {
                        var serial = cashSaleSerial.SerialNumber;
                        if (serial is null)
                        {
                            continue;
                        }

                        serial.Status = "InStock";
                        serial.CurrentCustomerId = null;
                        serial.CustomerWarrantyStartDate = null;
                        serial.CustomerWarrantyEndDate = null;
                    }
                }
            }

            var cashSaleSerials = cashSale.CashSaleDetails
                .SelectMany(x => x.CashSaleSerials)
                .ToList();
            _context.CashSaleSerials.RemoveRange(cashSaleSerials);

            cashSale.Status = "Cancelled";
            cashSale.UpdatedDate = DateTime.UtcNow;
            cashSale.UpdatedByUserId = CurrentUserId();
            cashSale.CancelledByUserId = CurrentUserId();
            cashSale.CancelledDate = DateTime.UtcNow;
            cashSale.CancelReason = cancelReason.Trim();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["CashSaleNotice"] = "Cash sale cancelled successfully.";
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["CashSaleNotice"] = "Cash sale cancellation failed. No changes were saved.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var cashSale = await _context.CashSaleHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Branch)
            .Include(x => x.TreatmentRight)
            .Include(x => x.ReferringDoctor)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.CashSaleDetails)
                .ThenInclude(x => x.CashSaleSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.CashSaleId == id.Value);

        if (cashSale is null || !CanAccessBranch(cashSale.BranchId))
        {
            return NotFound();
        }

        PopulatePrintCompanyViewData(_companyProfile);
        ViewData["EnablePatientInfo"] = await _systemSettingService.GetEnablePatientInfoAsync();
        return View(cashSale);
    }

    private async Task PostStockAsync(CashSaleHeader cashSale)
    {
        var itemIds = cashSale.CashSaleDetails.Select(x => x.ItemId).Distinct().ToList();
        var serialIds = cashSale.CashSaleDetails
            .SelectMany(x => x.CashSaleSerials)
            .Select(x => x.SerialId)
            .Distinct()
            .ToList();

        var itemMap = await _context.Items
            .Where(x => itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId);

        var serialMap = serialIds.Count == 0
            ? new Dictionary<int, SerialNumber>()
            : await _context.SerialNumbers
                .Where(x => serialIds.Contains(x.SerialId))
                .ToDictionaryAsync(x => x.SerialId);

        foreach (var detail in cashSale.CashSaleDetails)
        {
            var item = itemMap[detail.ItemId];
            var isProduct = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase);
            if (isProduct && item.TrackStock)
            {
                var branchStock = await GetBranchStockAsync(cashSale.BranchId, detail.ItemId);
                if (branchStock < detail.Qty)
                {
                    throw new InvalidOperationException($"Insufficient stock for item {item.ItemCode}. Branch stock is {branchStock:N2}.");
                }

                await AdjustStockBalanceAsync(cashSale.BranchId, detail.ItemId, -detail.Qty);
                _context.StockMovements.Add(new StockMovement
                {
                    MovementDate = cashSale.CashSaleDate,
                    MovementType = "CashSaleIssue",
                    ReferenceType = "CashSale",
                    ReferenceId = cashSale.CashSaleId,
                    ItemId = detail.ItemId,
                    FromBranchId = cashSale.BranchId,
                    Qty = -detail.Qty,
                    Remark = cashSale.CashSaleNo,
                    CreatedByUserId = CurrentUserId(),
                    CreatedDate = DateTime.UtcNow
                });
            }

            foreach (var cashSaleSerial in detail.CashSaleSerials)
            {
                var serial = serialMap[cashSaleSerial.SerialId];
                if (!string.Equals(serial.Status, "InStock", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Serial {serial.SerialNo} is no longer available.");
                }

                serial.Status = "Sold";
                serial.CurrentCustomerId = cashSale.CustomerId;
                serial.CustomerWarrantyStartDate = detail.CustomerWarrantyStartDate?.Date;
                serial.CustomerWarrantyEndDate = detail.CustomerWarrantyEndDate?.Date;
            }
        }
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

    private async Task AdjustStockBalanceAsync(int? branchId, int itemId, decimal qtyDelta)
    {
        if (!branchId.HasValue || qtyDelta == 0)
        {
            return;
        }

        var balance = await _context.StockBalances
            .FirstOrDefaultAsync(x => x.BranchId == branchId.Value && x.ItemId == itemId);

        if (balance is null)
        {
            balance = new StockBalance
            {
                BranchId = branchId.Value,
                ItemId = itemId,
                QtyOnHand = 0
            };
            _context.StockBalances.Add(balance);
        }

        balance.QtyOnHand += qtyDelta;
    }

    private async Task<decimal> GetBranchStockAsync(int? branchId, int itemId)
    {
        if (!branchId.HasValue)
        {
            return 0m;
        }

        return await _context.StockBalances
            .Where(x => x.BranchId == branchId.Value && x.ItemId == itemId)
            .SumAsync(x => (decimal?)x.QtyOnHand) ?? 0m;
    }

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private async Task PopulateLookupsAsync(InvoiceFormViewModel model)
    {
        var pricingMode = await _systemSettingService.GetPricingModeAsync();
        var enablePatientInfo = await _systemSettingService.GetEnablePatientInfoAsync();
        model.PricingMode = pricingMode;
        model.ShowPriceLevelSelector = string.Equals(pricingMode, PricingModes.MultiPrice, StringComparison.OrdinalIgnoreCase);
        model.ShowPatientInfo = enablePatientInfo;
        model.QuotationId = null;
        model.QuotationNo = null;
        model.AmountDueThisInvoice = null;

        if (!model.ShowPriceLevelSelector)
        {
            model.PriceLevelId = null;
        }

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

        model.PriceLevelOptions = model.ShowPriceLevelSelector
            ? await _context.PriceLevels
                .AsNoTracking()
                .Where(x => x.IsActive || x.PriceLevelId == model.PriceLevelId)
                .OrderBy(x => x.PriceLevelName)
                .ThenBy(x => x.PriceLevelCode)
                .Select(x => new SelectListItem
                {
                    Value = x.PriceLevelId.ToString(),
                    Text = $"{x.PriceLevelCode} - {x.PriceLevelName}",
                    Selected = model.PriceLevelId.HasValue && x.PriceLevelId == model.PriceLevelId.Value
                })
                .ToListAsync()
            : Array.Empty<SelectListItem>();

        model.BranchName = branches.FirstOrDefault(x => x.BranchId == model.BranchId)?.BranchName ?? "No Branch";

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

        model.PatientGenderOptions = new[]
        {
            new SelectListItem("ไม่ระบุ", string.Empty, string.IsNullOrWhiteSpace(model.PatientGender)),
            new SelectListItem("ชาย", "ชาย", model.PatientGender == "ชาย"),
            new SelectListItem("หญิง", "หญิง", model.PatientGender == "หญิง"),
            new SelectListItem("อื่นๆ", "อื่นๆ", model.PatientGender == "อื่นๆ")
        };

        model.TreatmentRightOptions = await _context.TreatmentRights
            .AsNoTracking()
            .Where(x => x.IsActive || x.TreatmentRightId == model.TreatmentRightId)
            .OrderBy(x => x.TreatmentRightCode)
            .Select(x => new SelectListItem
            {
                Value = x.TreatmentRightId.ToString(),
                Text = $"{x.TreatmentRightCode} - {x.TreatmentRightName}",
                Selected = model.TreatmentRightId.HasValue && x.TreatmentRightId == model.TreatmentRightId.Value
            })
            .ToListAsync();

        model.ReferringDoctorOptions = await _context.ReferringDoctors
            .AsNoTracking()
            .Where(x => x.IsActive || x.ReferringDoctorId == model.ReferringDoctorId)
            .OrderBy(x => x.DoctorCode)
            .Select(x => new SelectListItem
            {
                Value = x.ReferringDoctorId.ToString(),
                Text = $"{x.DoctorCode} - {x.DoctorName}",
                Selected = model.ReferringDoctorId.HasValue && x.ReferringDoctorId == model.ReferringDoctorId.Value
            })
            .ToListAsync();
        model.QuotationOptions = Array.Empty<SelectListItem>();

        model.VatTypeOptions = new[]
        {
            new SelectListItem("ราคายังไม่รวม VAT", VatModeHelper.VatExclusive),
            new SelectListItem("ราคารวม VAT", VatModeHelper.VatInclusive),
            new SelectListItem("No VAT", VatModeHelper.NoVat)
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
                CustomerCode = x.CustomerCode,
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

        var itemPriceMap = await _context.ItemPrices
            .AsNoTracking()
            .Include(x => x.PriceLevel)
            .Where(x => x.PriceLevel != null && x.PriceLevel.IsActive)
            .GroupBy(x => x.ItemId)
            .ToDictionaryAsync(
                x => x.Key,
                x => x.ToDictionary(y => y.PriceLevelId, y => y.UnitPrice));

        var itemQuery = _context.Items
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ItemCode);

        List<InvoiceItemLookupViewModel> itemLookup;
        if (model.BranchId.HasValue)
        {
            var selectedBranchId = model.BranchId.Value;
            var items = await itemQuery
                .Select(x => new
                {
                    x.ItemId,
                    x.ItemCode,
                    x.ItemName,
                    x.PartNumber,
                    x.ItemType,
                    x.UnitPrice,
                    x.TrackStock,
                    x.IsSerialControlled,
                    CurrentStock = _context.StockBalances
                        .Where(b => b.ItemId == x.ItemId && b.BranchId == selectedBranchId)
                        .Sum(b => (decimal?)b.QtyOnHand) ?? 0
                })
                .ToListAsync();

            itemLookup = items
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
                .ToList();
        }
        else
        {
            var items = await itemQuery
                .Select(x => new
                {
                    x.ItemId,
                    x.ItemCode,
                    x.ItemName,
                    x.PartNumber,
                    x.ItemType,
                    x.UnitPrice,
                    x.TrackStock,
                    x.IsSerialControlled,
                    x.CurrentStock
                })
                .ToListAsync();

            itemLookup = items
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
                .ToList();
        }

        model.ItemLookup = itemLookup;

        foreach (var item in model.ItemLookup)
        {
            item.PriceLevelPrices = itemPriceMap.TryGetValue(item.ItemId, out var levelPrices)
                ? levelPrices
                : new Dictionary<int, decimal>();
        }

        var stockByItemId = model.ItemLookup
            .GroupBy(x => x.ItemId)
            .ToDictionary(x => x.Key, x => x.First().CurrentStock);

        foreach (var detail in model.Details)
        {
            if (!detail.ItemId.HasValue)
            {
                detail.CurrentStock = 0m;
                continue;
            }

            detail.CurrentStock = stockByItemId.TryGetValue(detail.ItemId.Value, out var currentStock)
                ? currentStock
                : 0m;
        }

        var selectedSerialIds = model.Details
            .SelectMany(x => x.SelectedSerialIds)
            .Distinct()
            .ToList();

        var serialQuery = _context.SerialNumbers
            .AsNoTracking()
            .Where(x => x.Status == "InStock" || selectedSerialIds.Contains(x.SerialId));

        if (model.BranchId.HasValue)
        {
            var selectedBranchId = model.BranchId.Value;
            serialQuery = serialQuery.Where(x => x.BranchId == selectedBranchId || selectedSerialIds.Contains(x.SerialId));
        }

        model.SerialLookup = await serialQuery
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

    private static InvoiceFormViewModel MapCashSaleToForm(CashSaleHeader cashSale)
    {
        var detailDiscountTotal = cashSale.CashSaleDetails.Sum(x => x.DiscountAmount);
        var useHeaderDiscount = cashSale.DiscountAmount > 0 && detailDiscountTotal == 0;

        return new InvoiceFormViewModel
        {
            InvoiceId = cashSale.CashSaleId,
            InvoiceNo = cashSale.CashSaleNo,
            InvoiceDate = cashSale.CashSaleDate,
            CustomerId = cashSale.CustomerId,
            SalespersonId = cashSale.SalespersonId,
            BranchId = cashSale.BranchId,
            PriceLevelId = cashSale.PriceLevelId,
            BranchName = cashSale.Branch?.BranchName ?? string.Empty,
            ReferenceNo = cashSale.ReferenceNo,
            PatientFullName = cashSale.PatientFullName,
            PatientAge = cashSale.PatientAge,
            PatientGender = cashSale.PatientGender,
            PatientHn = cashSale.PatientHn,
            TreatmentRightId = cashSale.TreatmentRightId,
            PatientWard = cashSale.PatientWard,
            ReferringDoctorId = cashSale.ReferringDoctorId,
            VatType = VatModeHelper.Normalize(cashSale.VatType, VatModeHelper.VatExclusive),
            DiscountMode = useHeaderDiscount ? "Header" : "Line",
            HeaderDiscountAmount = useHeaderDiscount ? cashSale.DiscountAmount : 0m,
            Remark = cashSale.Remark,
            Subtotal = cashSale.Subtotal,
            DiscountAmount = useHeaderDiscount ? 0m : cashSale.DiscountAmount,
            VatAmount = cashSale.VatAmount,
            TotalAmount = cashSale.TotalAmount,
            ReferenceSubtotal = cashSale.Subtotal,
            ReferenceDiscountAmount = cashSale.DiscountAmount,
            ReferenceVatAmount = cashSale.VatAmount,
            ReferenceTotalAmount = cashSale.TotalAmount,
            PaidAmount = cashSale.TotalAmount,
            BalanceAmount = 0m,
            Status = cashSale.Status,
            ShowPatientInfo = true,
            Details = cashSale.CashSaleDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new InvoiceLineEditorViewModel
                {
                    InvoiceDetailId = x.CashSaleDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    ItemCode = x.Item?.ItemCode ?? string.Empty,
                    ItemName = x.Item?.ItemName ?? string.Empty,
                    PartNumber = x.Item?.PartNumber ?? string.Empty,
                    ItemType = x.Item?.ItemType ?? "Product",
                    TrackStock = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.TrackStock ?? false),
                    IsSerialControlled = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.IsSerialControlled ?? false),
                    CurrentStock = x.Item?.CurrentStock ?? 0m,
                    QuotedQty = x.Qty,
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice,
                    DiscountAmount = x.DiscountAmount,
                    LineTotal = x.LineTotal,
                    Remark = x.Remark,
                    CustomerWarrantyStartDate = x.CustomerWarrantyStartDate,
                    CustomerWarrantyEndDate = x.CustomerWarrantyEndDate,
                    SelectedSerialIds = x.CashSaleSerials.Select(s => s.SerialId).ToList()
                })
                .ToList()
        };
    }

    private async Task<bool> ValidateAndComputeAsync(InvoiceFormViewModel model, bool requireSerials, bool requireAvailableStock)
    {
        EnsureAtLeastOneLine(model);

        if (!model.CustomerId.HasValue)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "กรุณาเลือกลูกค้า");
        }

        if (!model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "กรุณาเลือกสาขา");
        }
        else if (!CanAccessBranch(model.BranchId))
        {
            ModelState.AddModelError(nameof(model.BranchId), "คุณไม่มีสิทธิ์ใช้งานสาขาที่เลือก");
        }

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "กรุณาเพิ่มอย่างน้อย 1 รายการ");
        }

        var itemIds = model.Details
            .Where(x => x.ItemId.HasValue)
            .Select(x => x.ItemId!.Value)
            .Distinct()
            .ToList();

        var itemMap = itemIds.Count == 0
            ? new Dictionary<int, Item>()
            : await _context.Items
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ItemId))
                .ToDictionaryAsync(x => x.ItemId);

        var itemPriceMap = await _context.ItemPrices
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .GroupBy(x => x.ItemId)
            .ToDictionaryAsync(
                x => x.Key,
                x => x.ToDictionary(y => y.PriceLevelId, y => y.UnitPrice));

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
        decimal lineDiscountTotal = 0m;
        var useLineDiscount = model.DiscountMode == "Line";

        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            detail.LineNumber = i + 1;
            detail.QuotedQty = detail.Qty;

            if (detail.Qty <= 0)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "จำนวนต้องมากกว่า 0");
            }

            if (!detail.ItemId.HasValue || !itemMap.TryGetValue(detail.ItemId.Value, out var item))
            {
                ModelState.AddModelError($"Details[{i}].ItemId", "กรุณาเลือกรายการสินค้าที่ถูกต้อง");
                continue;
            }

            detail.ItemCode = item.ItemCode;
            detail.ItemName = item.ItemName;
            detail.PartNumber = item.PartNumber;
            detail.ItemType = item.ItemType;
            detail.TrackStock = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase) && item.TrackStock;
            detail.IsSerialControlled = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase) && item.IsSerialControlled;
            detail.CurrentStock = detail.TrackStock && model.BranchId.HasValue
                ? await _context.StockBalances
                    .Where(x => x.ItemId == item.ItemId && x.BranchId == model.BranchId.Value)
                    .SumAsync(x => (decimal?)x.QtyOnHand) ?? 0m
                : detail.TrackStock ? item.CurrentStock : 0m;

            if (detail.UnitPrice <= 0)
            {
                detail.UnitPrice = ResolveUnitPrice(item, model.PriceLevelId, itemPriceMap);
            }

            if (requireAvailableStock && detail.TrackStock && detail.CurrentStock < detail.Qty)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "จำนวนต้องไม่มากกว่าสต๊อกคงเหลือ");
            }

            var gross = detail.Qty * detail.UnitPrice;
            if (!useLineDiscount)
            {
                detail.DiscountAmount = 0m;
            }

            if (detail.DiscountAmount > gross)
            {
                ModelState.AddModelError($"Details[{i}].DiscountAmount", "ส่วนลดต่อรายการต้องไม่มากกว่ายอดก่อนหักส่วนลด");
                continue;
            }

            if (detail.IsSerialControlled)
            {
                if (detail.Qty != Math.Truncate(detail.Qty))
                {
                    ModelState.AddModelError($"Details[{i}].Qty", "สินค้าที่คุม Serial ต้องใช้จำนวนเต็มเท่านั้น");
                }

                if (detail.SelectedSerialIds.Count > (int)detail.Qty)
                {
                    ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "จำนวน Serial ที่เลือกต้องไม่มากกว่าจำนวนสินค้า");
                }

                if (requireSerials && detail.SelectedSerialIds.Count == 0)
                {
                    ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "กรุณาเลือก Serial ก่อนออกเอกสาร");
                }

                if (requireSerials && detail.SelectedSerialIds.Count != (int)detail.Qty)
                {
                    ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "จำนวน Serial ที่เลือกต้องเท่ากับจำนวนสินค้า");
                }

                if (detail.CustomerWarrantyStartDate.HasValue &&
                    detail.CustomerWarrantyEndDate.HasValue &&
                    detail.CustomerWarrantyEndDate.Value.Date < detail.CustomerWarrantyStartDate.Value.Date)
                {
                    ModelState.AddModelError($"Details[{i}].CustomerWarrantyEndDate", "วันที่สิ้นสุดประกันต้องไม่น้อยกว่าวันที่เริ่มประกัน");
                }

                foreach (var serialId in detail.SelectedSerialIds)
                {
                    if (duplicateSerialIds.Contains(serialId))
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "ไม่สามารถเลือก Serial เดิมซ้ำได้");
                    }

                    if (!serialMap.TryGetValue(serialId, out var serial))
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "ไม่พบ Serial ที่เลือกอย่างน้อย 1 รายการ");
                        continue;
                    }

                    if (serial.ItemId != item.ItemId)
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Serial ที่เลือกไม่ตรงกับสินค้าที่เลือก");
                    }

                    if (!string.Equals(serial.Status, "InStock", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "สามารถใช้ได้เฉพาะ Serial ที่มีสถานะ InStock");
                    }

                    if (model.BranchId.HasValue && serial.BranchId != model.BranchId.Value)
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Serial ที่เลือกไม่ได้อยู่ในสาขาที่เลือก");
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
            subtotal += gross;
            if (useLineDiscount)
            {
                lineDiscountTotal += detail.DiscountAmount;
            }
        }

        if (useLineDiscount)
        {
            model.HeaderDiscountAmount = 0m;
        }

        if (!useLineDiscount && model.HeaderDiscountAmount > subtotal)
        {
            ModelState.AddModelError(nameof(model.HeaderDiscountAmount), "ส่วนลดท้ายเอกสารต้องไม่มากกว่ายอดก่อน VAT");
        }

        model.VatType = VatModeHelper.Normalize(model.VatType, VatModeHelper.VatExclusive);
        var appliedDiscount = useLineDiscount ? lineDiscountTotal : model.HeaderDiscountAmount;
        var net = subtotal - appliedDiscount;
        var vatComputation = VatModeHelper.ComputeFromDocumentPricing(net, model.VatType);
        var vat = vatComputation.VatAmount;
        var total = vatComputation.TotalAmount;

        model.Subtotal = subtotal;
        model.DiscountAmount = useLineDiscount ? lineDiscountTotal : 0m;
        model.VatAmount = vat;
        model.TotalAmount = total;
        model.ReferenceSubtotal = subtotal;
        model.ReferenceDiscountAmount = appliedDiscount;
        model.ReferenceVatAmount = vat;
        model.ReferenceTotalAmount = total;
        model.PaidAmount = total;
        model.BalanceAmount = 0m;
        model.AmountDueThisInvoice = null;
        EnsureAtLeastOneLine(model);
        return ModelState.IsValid;
    }

    private string BuildIssueValidationNotice()
    {
        var errors = ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (errors.Count == 0)
        {
            return "ไม่สามารถออกเอกสารขายสดได้ กรุณาตรวจสอบข้อมูลเอกสารแล้วลองใหม่อีกครั้ง";
        }

        return $"ไม่สามารถออกเอกสารขายสดได้ {string.Join(" ", errors)}";
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
                x.QuotedQty = x.Qty > 0 ? x.Qty : 1m;
                x.SelectedSerialIds = x.SelectedSerialIds.Distinct().ToList();
                return x;
            })
            .ToList();
    }

    private static CashSaleDetail MapDetailEntity(InvoiceLineEditorViewModel detail)
    {
        return new CashSaleDetail
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
            CashSaleSerials = detail.SelectedSerialIds
                .Select(serialId => new CashSaleSerial
                {
                    SerialId = serialId
                })
                .ToList()
        };
    }

    private static decimal ResolveUnitPrice(
        Item item,
        int? priceLevelId,
        IReadOnlyDictionary<int, Dictionary<int, decimal>> itemPriceMap)
    {
        if (priceLevelId.HasValue &&
            itemPriceMap.TryGetValue(item.ItemId, out var priceByLevel) &&
            priceByLevel.TryGetValue(priceLevelId.Value, out var levelPrice) &&
            levelPrice > 0)
        {
            return levelPrice;
        }

        return item.UnitPrice;
    }

    private Task<string> GetNextCashSaleNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.CashSaleHeaders.Select(x => x.CashSaleNo), prefix, date);
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(NumberPrefix, date, nextSequence);
    }
}
