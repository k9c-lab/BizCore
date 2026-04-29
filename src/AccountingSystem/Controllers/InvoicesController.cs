using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BizCore.Controllers;

[Authorize]
public class InvoicesController : CrudControllerBase
{
    private const string NumberPrefix = "INV";
    private readonly AccountingDbContext _context;
    private readonly CompanyProfileSettings _companyProfile;

    public InvoicesController(AccountingDbContext context, IOptions<CompanyProfileSettings> companyProfileOptions)
    {
        _context = context;
        _companyProfile = companyProfileOptions.Value;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.InvoiceHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Branch)
            .Include(x => x.Quotation)
            .Include(x => x.PaymentAllocations)
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
                x.InvoiceNo.Contains(keyword) ||
                (x.ReferenceNo != null && x.ReferenceNo.Contains(keyword)) ||
                (x.Customer != null && (
                    x.Customer.CustomerCode.Contains(keyword) ||
                    x.Customer.CustomerName.Contains(keyword) ||
                    (x.Customer.TaxId != null && x.Customer.TaxId.Contains(keyword)))) ||
                (x.Quotation != null && x.Quotation.QuotationNumber.Contains(keyword)) ||
                (x.Salesperson != null && x.Salesperson.SalespersonName.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.InvoiceDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.InvoiceDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var invoices = await PaginatedList<InvoiceHeader>.CreateAsync(query
            .OrderByDescending(x => x.InvoiceDate)
            .ThenByDescending(x => x.InvoiceId), page, pageSize);

        return View(invoices);
    }

    public async Task<IActionResult> Create(int? quotationId)
    {
        var model = new InvoiceFormViewModel
        {
            InvoiceNo = await GetNextInvoiceNumberAsync(DateTime.Today),
            Status = "Draft",
            DiscountMode = "Line",
            BranchId = CurrentBranchId()
        };

        if (quotationId.HasValue)
        {
            var quotation = await _context.QuotationHeaders
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.QuotationDetails)
                    .ThenInclude(x => x.Item)
                .FirstOrDefaultAsync(x => x.QuotationHeaderId == quotationId.Value);

            if (quotation is null || !CanAccessBranch(quotation.BranchId))
            {
                return NotFound();
            }

            if (quotation.Status is not ("Approved" or "Converted"))
            {
                TempData["InvoiceNotice"] = "Only approved quotations can be used to prefill an invoice.";
                return RedirectToAction("Details", "Quotations", new { id = quotation.QuotationHeaderId });
            }

            var remainingAmount = await GetQuotationRemainingAmountAsync(
                quotation.QuotationHeaderId,
                quotation.TotalAmount,
                excludeInvoiceId: null);
            var quotationLineProgress = await GetQuotationLineProgressMapAsync(
                quotation.QuotationDetails.Select(x => x.QuotationDetailId),
                excludeInvoiceId: null);
            ApplyQuotationToModel(model, quotation, remainingAmount, quotationLineProgress);
        }

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
                .ThenInclude(x => x!.QuotationDetails)
            .Include(x => x.Branch)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
            .FirstOrDefaultAsync(x => x.InvoiceId == id.Value);

        if (invoice is null || !CanAccessBranch(invoice.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["InvoiceNotice"] = "Only draft invoices can be edited.";
            return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
        }

        var model = MapInvoiceToForm(invoice);
        if (invoice.QuotationId.HasValue)
        {
            var quotationLineIds = invoice.InvoiceDetails
                .Where(x => x.QuotationDetailId.HasValue)
                .Select(x => x.QuotationDetailId!.Value)
                .Distinct()
                .ToList();
            var quotationLineProgress = await GetQuotationLineProgressMapAsync(quotationLineIds, invoice.InvoiceId);
            ApplyQuotationLineProgress(model, quotationLineProgress);
        }
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

        if (invoice is null || !CanAccessBranch(invoice.BranchId))
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
        model.BranchId = invoice.BranchId;
        ModelState.Remove(nameof(InvoiceFormViewModel.InvoiceNo));

        if (!await ValidateAndComputeAsync(model, requireSerials: false, requireAvailableStock: false))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        invoice.InvoiceDate = model.InvoiceDate.Date;
        invoice.CustomerId = model.CustomerId!.Value;
        invoice.SalespersonId = model.SalespersonId;
        invoice.BranchId = model.BranchId;
        invoice.QuotationId = model.QuotationId;
        invoice.ReferenceNo = model.ReferenceNo?.Trim();
        invoice.Remark = model.Remark?.Trim();
        invoice.Subtotal = model.Subtotal;
        invoice.DiscountAmount = model.DiscountMode == "Header" ? model.HeaderDiscountAmount : model.DiscountAmount;
        invoice.VatType = model.VatType;
        invoice.VatAmount = model.VatAmount;
        invoice.TotalAmount = model.TotalAmount;
        invoice.ReferenceLineSubtotal = CalculateReferenceSubtotal(model.Details);
        invoice.ReferenceLineDiscountAmount = model.DiscountMode == "Header"
            ? model.HeaderDiscountAmount
            : CalculateReferenceLineDiscount(model.Details);
        invoice.ReferenceLineVatAmount = model.VatType == "VAT"
            ? Math.Round((invoice.ReferenceLineSubtotal.Value - invoice.ReferenceLineDiscountAmount.Value) * 0.07m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        invoice.ReferenceLineTotalAmount = (invoice.ReferenceLineSubtotal ?? 0m) - (invoice.ReferenceLineDiscountAmount ?? 0m) + (invoice.ReferenceLineVatAmount ?? 0m);
        invoice.PaidAmount = 0m;
        invoice.BalanceAmount = model.BalanceAmount;
        invoice.Status = "Draft";
        invoice.UpdatedDate = DateTime.UtcNow;
        invoice.UpdatedByUserId = CurrentUserId();

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
    public async Task<IActionResult> Create(InvoiceFormViewModel model, string? submitAction)
    {
        var issueInvoice = string.Equals(submitAction, "Issue", StringComparison.OrdinalIgnoreCase);
        model.InvoiceNo = await GetNextInvoiceNumberAsync(model.InvoiceDate);
        model.Status = issueInvoice ? "Issued" : "Draft";
        model.PaidAmount = 0m;
        if (!CurrentUserCanAccessAllBranches())
        {
            model.BranchId = CurrentBranchId();
        }
        ModelState.Remove(nameof(InvoiceFormViewModel.InvoiceNo));

        if (!await ValidateAndComputeAsync(model, requireSerials: issueInvoice, requireAvailableStock: issueInvoice))
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
                BranchId = model.BranchId,
                QuotationId = model.QuotationId,
                ReferenceNo = model.ReferenceNo?.Trim(),
                Remark = model.Remark?.Trim(),
                Subtotal = model.Subtotal,
                DiscountAmount = model.DiscountMode == "Header" ? model.HeaderDiscountAmount : model.DiscountAmount,
                VatType = model.VatType,
                VatAmount = model.VatAmount,
                TotalAmount = model.TotalAmount,
                PaidAmount = 0m,
                BalanceAmount = model.BalanceAmount,
                ReferenceLineSubtotal = CalculateReferenceSubtotal(model.Details),
                ReferenceLineDiscountAmount = model.DiscountMode == "Header"
                    ? model.HeaderDiscountAmount
                    : CalculateReferenceLineDiscount(model.Details),
                Status = issueInvoice ? "Issued" : "Draft",
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = CurrentUserId(),
                IssuedByUserId = issueInvoice ? CurrentUserId() : null,
                IssuedDate = issueInvoice ? DateTime.UtcNow : null,
                InvoiceDetails = model.Details.Select(MapDetailEntity).ToList()
            };

            header.ReferenceLineVatAmount = model.VatType == "VAT"
                ? Math.Round(((header.ReferenceLineSubtotal ?? 0m) - (header.ReferenceLineDiscountAmount ?? 0m)) * 0.07m, 2, MidpointRounding.AwayFromZero)
                : 0m;
            header.ReferenceLineTotalAmount = (header.ReferenceLineSubtotal ?? 0m) - (header.ReferenceLineDiscountAmount ?? 0m) + (header.ReferenceLineVatAmount ?? 0m);

            _context.InvoiceHeaders.Add(header);
            await _context.SaveChangesAsync();

            if (issueInvoice)
            {
                foreach (var detail in model.Details)
                {
                    var item = itemMap[detail.ItemId!.Value];
                    var isProduct = string.Equals(item.ItemType, "Product", StringComparison.OrdinalIgnoreCase);
                    if (isProduct && item.TrackStock)
                    {
                        var branchStock = await GetBranchStockAsync(model.BranchId, detail.ItemId!.Value);
                        if (branchStock < detail.Qty)
                        {
                            throw new InvalidOperationException($"Insufficient stock for item {item.ItemCode}. Branch stock is {branchStock:N2}.");
                        }

                        await AdjustStockBalanceAsync(model.BranchId, detail.ItemId!.Value, -detail.Qty);
                        _context.StockMovements.Add(new StockMovement
                        {
                            MovementDate = header.InvoiceDate,
                            MovementType = "InvoiceIssue",
                            ReferenceType = "Invoice",
                            ReferenceId = header.InvoiceId,
                            ItemId = detail.ItemId!.Value,
                            FromBranchId = model.BranchId,
                            Qty = -detail.Qty,
                            Remark = header.InvoiceNo,
                            CreatedByUserId = CurrentUserId(),
                            CreatedDate = DateTime.UtcNow
                        });
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
            .Include(x => x.Branch)
            .Include(x => x.Quotation)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.IssuedByUser)
            .Include(x => x.CancelledByUser)
            .Include(x => x.PaymentAllocations)
            .Include(x => x.Branch)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.InvoiceId == id.Value);

        return invoice is null || !CanAccessBranch(invoice.BranchId) ? NotFound() : View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(int id)
    {
        var invoice = await _context.InvoiceHeaders
            .Include(x => x.Quotation)
            .Include(x => x.Branch)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
            .FirstOrDefaultAsync(x => x.InvoiceId == id);

        if (invoice is null || !CanAccessBranch(invoice.BranchId))
        {
            return NotFound();
        }

        if (!string.Equals(invoice.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            TempData["InvoiceNotice"] = "Only draft invoices can be issued.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = MapInvoiceToForm(invoice);
        if (!await ValidateAndComputeAsync(model, requireSerials: true, requireAvailableStock: true))
        {
            TempData["InvoiceNotice"] = "Cannot issue invoice. Serial-controlled items must have selected serial count equal to quantity.";
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
                    var branchStock = await GetBranchStockAsync(invoice.BranchId, detail.ItemId);
                    if (branchStock < detail.Qty)
                    {
                        throw new InvalidOperationException($"Insufficient stock for item {item.ItemCode}. Branch stock is {branchStock:N2}.");
                    }

                    await AdjustStockBalanceAsync(invoice.BranchId, detail.ItemId, -detail.Qty);
                    _context.StockMovements.Add(new StockMovement
                    {
                        MovementDate = invoice.InvoiceDate,
                        MovementType = "InvoiceIssue",
                        ReferenceType = "Invoice",
                        ReferenceId = invoice.InvoiceId,
                        ItemId = detail.ItemId,
                        FromBranchId = invoice.BranchId,
                        Qty = -detail.Qty,
                        Remark = invoice.InvoiceNo,
                        CreatedByUserId = CurrentUserId(),
                        CreatedDate = DateTime.UtcNow
                    });
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
            invoice.UpdatedByUserId = CurrentUserId();
            invoice.IssuedByUserId = CurrentUserId();
            invoice.IssuedDate = DateTime.UtcNow;
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
    public async Task<IActionResult> Cancel(int id, string? cancelReason)
    {
        var invoice = await _context.InvoiceHeaders
            .Include(x => x.PaymentAllocations)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.Item)
            .Include(x => x.InvoiceDetails)
                .ThenInclude(x => x.InvoiceSerials)
                    .ThenInclude(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.InvoiceId == id);

        if (invoice is null || !CanAccessBranch(invoice.BranchId))
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
                        await AdjustStockBalanceAsync(invoice.BranchId, detail.ItemId, detail.Qty);
                        _context.StockMovements.Add(new StockMovement
                        {
                            MovementDate = DateTime.Today,
                            MovementType = "InvoiceCancel",
                            ReferenceType = "Invoice",
                            ReferenceId = invoice.InvoiceId,
                            ItemId = detail.ItemId,
                            ToBranchId = invoice.BranchId,
                            Qty = detail.Qty,
                            Remark = invoice.InvoiceNo,
                            CreatedByUserId = CurrentUserId(),
                            CreatedDate = DateTime.UtcNow
                        });
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
            invoice.UpdatedByUserId = CurrentUserId();
            invoice.CancelledByUserId = CurrentUserId();
            invoice.CancelledDate = DateTime.UtcNow;
            invoice.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? null : cancelReason.Trim();
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
            .Include(x => x.Branch)
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

        PopulatePrintCompanyViewData(_companyProfile);
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

        var quotationQuery = _context.QuotationHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x =>
                x.Status == "Approved" ||
                x.Status == "Converted" ||
                (model.QuotationId.HasValue && x.QuotationHeaderId == model.QuotationId.Value));

        if (!canAccessAllBranches)
        {
            var currentBranchId = CurrentBranchId();
            quotationQuery = quotationQuery.Where(x => x.BranchId == currentBranchId);
        }

        var quotations = await quotationQuery
            .OrderByDescending(x => x.QuotationDate)
            .ThenByDescending(x => x.QuotationHeaderId)
            .ToListAsync();

        model.QuotationOptions = quotations
            .Select(x => new SelectListItem
            {
                Value = x.QuotationHeaderId.ToString(),
                Text = $"{x.QuotationNumber} - {x.Customer?.CustomerName ?? "-"}",
                Selected = model.QuotationId.HasValue && x.QuotationHeaderId == model.QuotationId.Value
            })
            .ToList();

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

        var itemQuery = _context.Items
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ItemCode);

        if (model.BranchId.HasValue)
        {
            var selectedBranchId = model.BranchId.Value;
            model.ItemLookup = await itemQuery
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
                    CurrentStock = _context.StockBalances
                        .Where(b => b.ItemId == x.ItemId && b.BranchId == selectedBranchId)
                        .Sum(b => (decimal?)b.QtyOnHand) ?? 0
                })
                .ToListAsync();
        }
        else
        {
            model.ItemLookup = await itemQuery
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

    private static InvoiceFormViewModel MapInvoiceToForm(InvoiceHeader invoice)
    {
        var detailDiscountTotal = invoice.InvoiceDetails.Sum(x => x.DiscountAmount);
        var referenceDiscountTotal = invoice.ReferenceLineDiscountAmount ?? invoice.DiscountAmount;
        var useHeaderDiscount = invoice.QuotationId.HasValue
            ? false
            : referenceDiscountTotal > 0 && detailDiscountTotal == 0;

        return new InvoiceFormViewModel
        {
            InvoiceId = invoice.InvoiceId,
            QuotationId = invoice.QuotationId,
            InvoiceNo = invoice.InvoiceNo,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            SalespersonId = invoice.SalespersonId,
            BranchId = invoice.BranchId,
            BranchName = invoice.Branch?.BranchName ?? string.Empty,
            QuotationNo = invoice.Quotation?.QuotationNumber,
            ReferenceNo = invoice.ReferenceNo,
            VatType = invoice.VatType,
            DiscountMode = invoice.QuotationId.HasValue ? "Line" : (useHeaderDiscount ? "Header" : "Line"),
            HeaderDiscountAmount = invoice.QuotationId.HasValue ? 0m : (useHeaderDiscount ? referenceDiscountTotal : 0m),
            AmountDueThisInvoice = invoice.TotalAmount,
            Remark = invoice.Remark,
            Subtotal = invoice.Subtotal,
            DiscountAmount = invoice.QuotationId.HasValue ? 0m : (useHeaderDiscount ? 0m : invoice.DiscountAmount),
            VatAmount = invoice.VatAmount,
            TotalAmount = invoice.TotalAmount,
            ReferenceSubtotal = invoice.ReferenceLineSubtotal ?? invoice.Subtotal,
            ReferenceDiscountAmount = invoice.ReferenceLineDiscountAmount ?? invoice.DiscountAmount,
            ReferenceVatAmount = invoice.ReferenceLineVatAmount ?? invoice.VatAmount,
            ReferenceTotalAmount = invoice.ReferenceLineTotalAmount ?? invoice.TotalAmount,
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
                    QuotationDetailId = x.QuotationDetailId,
                    ItemCode = x.Item?.ItemCode ?? string.Empty,
                    ItemName = x.Item?.ItemName ?? string.Empty,
                    PartNumber = x.Item?.PartNumber ?? string.Empty,
                    ItemType = x.Item?.ItemType ?? "Product",
                    TrackStock = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.TrackStock ?? false),
                    IsSerialControlled = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.IsSerialControlled ?? false),
                    CurrentStock = x.Item?.CurrentStock ?? 0m,
                    QuotedQty = x.QuotedQty,
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

    private static void ApplyQuotationToModel(
        InvoiceFormViewModel model,
        QuotationHeader quotation,
        decimal remainingAmount,
        IReadOnlyDictionary<int, decimal> quotationLineProgress)
    {
        model.QuotationId = quotation.QuotationHeaderId;
        model.QuotationNo = quotation.QuotationNumber;
        model.InvoiceDate = DateTime.Today;
        model.CustomerId = quotation.CustomerId;
        model.SalespersonId = quotation.SalespersonId;
        model.BranchId = quotation.BranchId;
        model.BranchName = quotation.Branch?.BranchName ?? model.BranchName;
        model.ReferenceNo = quotation.ReferenceNo;
        model.Remark = quotation.Remarks;
        model.VatType = quotation.VatType;
        model.DiscountMode = "Line";
        model.HeaderDiscountAmount = 0m;
        model.Subtotal = quotation.Subtotal;
        model.DiscountAmount = 0m;
        model.VatAmount = quotation.VatAmount;
        model.TotalAmount = quotation.TotalAmount;
        model.ReferenceSubtotal = quotation.Subtotal;
        model.ReferenceDiscountAmount = quotation.DiscountAmount;
        model.ReferenceVatAmount = quotation.VatAmount;
        model.ReferenceTotalAmount = quotation.TotalAmount;
        model.AmountDueThisInvoice = remainingAmount;
        model.PaidAmount = 0m;
        model.BalanceAmount = remainingAmount;
        model.Details = quotation.QuotationDetails
            .OrderBy(x => x.LineNumber)
            .Select(x =>
            {
                var previouslyInvoicedQty = quotationLineProgress.TryGetValue(x.QuotationDetailId, out var invoicedQty)
                    ? invoicedQty
                    : 0m;
                var remainingQuotedQty = Math.Max(x.Quantity - previouslyInvoicedQty, 0m);
                return new InvoiceLineEditorViewModel
                {
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    QuotationDetailId = x.QuotationDetailId,
                    ItemCode = x.Item?.ItemCode ?? string.Empty,
                    ItemName = x.Item?.ItemName ?? string.Empty,
                    PartNumber = x.Item?.PartNumber ?? string.Empty,
                    ItemType = x.Item?.ItemType ?? "Product",
                    TrackStock = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.TrackStock ?? false),
                    IsSerialControlled = string.Equals(x.Item?.ItemType ?? "Product", "Product", StringComparison.OrdinalIgnoreCase) && (x.Item?.IsSerialControlled ?? false),
                    CurrentStock = x.Item?.CurrentStock ?? 0m,
                    QuotedQty = x.Quantity,
                    PreviouslyInvoicedQty = previouslyInvoicedQty,
                    RemainingQuotedQty = remainingQuotedQty,
                    Qty = remainingQuotedQty,
                    UnitPrice = x.UnitPrice,
                    DiscountAmount = 0m,
                    LineTotal = Math.Max(remainingQuotedQty * x.UnitPrice, 0m),
                    Remark = x.Description
                };
            })
            .Where(x => x.Qty > 0)
            .ToList();
    }

    private static void ApplyQuotationLineProgress(InvoiceFormViewModel model, IReadOnlyDictionary<int, decimal> quotationLineProgress)
    {
        foreach (var detail in model.Details)
        {
            if (!detail.QuotationDetailId.HasValue)
            {
                continue;
            }

            var previouslyInvoicedQty = quotationLineProgress.TryGetValue(detail.QuotationDetailId.Value, out var invoicedQty)
                ? invoicedQty
                : 0m;
            detail.PreviouslyInvoicedQty = previouslyInvoicedQty;
            detail.RemainingQuotedQty = Math.Max(detail.QuotedQty - previouslyInvoicedQty, 0m);
        }
    }

    private async Task<Dictionary<int, decimal>> GetQuotationLineProgressMapAsync(IEnumerable<int> quotationDetailIds, int? excludeInvoiceId)
    {
        var detailIds = quotationDetailIds.Distinct().ToList();
        if (detailIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        return await _context.InvoiceDetails
            .AsNoTracking()
            .Where(x =>
                x.QuotationDetailId.HasValue &&
                detailIds.Contains(x.QuotationDetailId.Value) &&
                x.InvoiceHeader != null &&
                x.InvoiceHeader.Status != "Draft" &&
                x.InvoiceHeader.Status != "Cancelled" &&
                (!excludeInvoiceId.HasValue || x.InvoiceId != excludeInvoiceId.Value))
            .GroupBy(x => x.QuotationDetailId!.Value)
            .Select(x => new
            {
                QuotationDetailId = x.Key,
                Qty = x.Sum(d => d.Qty)
            })
            .ToDictionaryAsync(x => x.QuotationDetailId, x => x.Qty);
    }

    private async Task<decimal> GetQuotationRemainingAmountAsync(int quotationId, decimal quotationTotalAmount, int? excludeInvoiceId)
    {
        var alreadyInvoicedAmount = await _context.InvoiceHeaders
            .AsNoTracking()
            .Where(x =>
                x.QuotationId == quotationId &&
                x.Status != "Draft" &&
                x.Status != "Cancelled" &&
                (!excludeInvoiceId.HasValue || x.InvoiceId != excludeInvoiceId.Value))
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m;

        return Math.Max(quotationTotalAmount - alreadyInvoicedAmount, 0m);
    }

    private async Task<bool> ValidateAndComputeAsync(InvoiceFormViewModel model, bool requireSerials, bool requireAvailableStock)
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

        decimal? quotationRemainingAmount = null;

        if (model.QuotationId.HasValue)
        {
            var quotation = await _context.QuotationHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.QuotationHeaderId == model.QuotationId.Value);

            if (quotation is null || !CanAccessBranch(quotation.BranchId))
            {
                ModelState.AddModelError(nameof(model.QuotationId), "Selected quotation was not found.");
            }
            else
            {
                model.QuotationNo = quotation.QuotationNumber;
                quotationRemainingAmount = await GetQuotationRemainingAmountAsync(
                    quotation.QuotationHeaderId,
                    quotation.TotalAmount,
                    model.InvoiceId);
            }
        }
        else
        {
            model.QuotationNo = null;
        }

        if (!CurrentUserCanAccessAllBranches())
        {
            model.BranchId = CurrentBranchId();
        }

        if (!model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Please select a branch.");
        }
        else if (!CanAccessBranch(model.BranchId))
        {
            ModelState.AddModelError(nameof(model.BranchId), "You cannot create or edit invoices for this branch.");
        }
        else if (!await _context.Branches.AnyAsync(x => x.BranchId == model.BranchId.Value && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.BranchId), "Selected branch was not found or inactive.");
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

        if (model.QuotationId.HasValue)
        {
            model.DiscountMode = "Line";
            model.HeaderDiscountAmount = 0m;
            model.DiscountAmount = 0m;
            foreach (var detail in model.Details)
            {
                detail.DiscountAmount = 0m;
            }
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

        decimal referenceSubtotal = 0m;
        decimal referenceDiscount = 0m;
        var useLineDiscount = model.DiscountMode == "Line";

        for (var i = 0; i < model.Details.Count; i++)
        {
            var detail = model.Details[i];
            detail.LineNumber = i + 1;

            if (detail.QuotedQty <= 0)
            {
                ModelState.AddModelError($"Details[{i}].QuotedQty", "Quoted quantity must be greater than zero.");
            }

            if (detail.Qty <= 0)
            {
                ModelState.AddModelError($"Details[{i}].Qty", "Quantity must be greater than zero.");
            }

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
            detail.CurrentStock = detail.TrackStock && model.BranchId.HasValue
                ? await _context.StockBalances
                    .Where(x => x.ItemId == item.ItemId && x.BranchId == model.BranchId.Value)
                    .SumAsync(x => (decimal?)x.QtyOnHand) ?? 0m
                : detail.TrackStock ? item.CurrentStock : 0m;

            if (detail.UnitPrice <= 0)
            {
                detail.UnitPrice = item.UnitPrice;
            }

            if (requireAvailableStock && detail.TrackStock && detail.CurrentStock < detail.Qty)
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

                if (detail.QuotedQty != Math.Truncate(detail.QuotedQty))
                {
                    ModelState.AddModelError($"Details[{i}].QuotedQty", "Quoted quantity must use whole numbers for serial-controlled items.");
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

                    if (model.BranchId.HasValue && serial.BranchId != model.BranchId.Value)
                    {
                        ModelState.AddModelError($"Details[{i}].SelectedSerialIds", "Selected serial does not belong to the invoice branch.");
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
                referenceSubtotal += gross;
                referenceDiscount += detail.DiscountAmount;
            }
            else
            {
                referenceSubtotal += gross;
            }
        }

        if (useLineDiscount)
        {
            model.HeaderDiscountAmount = 0m;
        }

        if (!useLineDiscount && model.HeaderDiscountAmount > referenceSubtotal)
        {
            ModelState.AddModelError(nameof(model.HeaderDiscountAmount), "Header discount cannot exceed subtotal.");
        }

        var referenceAppliedDiscount = useLineDiscount ? referenceDiscount : model.HeaderDiscountAmount;
        var referenceNet = referenceSubtotal - referenceAppliedDiscount;
        var referenceVat = model.VatType == "VAT"
            ? Math.Round(referenceNet * 0.07m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var referenceTotal = referenceNet + referenceVat;

        model.DiscountAmount = useLineDiscount ? referenceDiscount : 0m;

        if (model.QuotationId.HasValue && model.AmountDueThisInvoice.HasValue && quotationRemainingAmount.HasValue)
        {
            if (model.AmountDueThisInvoice.Value > quotationRemainingAmount.Value)
            {
                ModelState.AddModelError(nameof(model.AmountDueThisInvoice), "Amount due cannot exceed the remaining amount from the quotation.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.Subtotal = referenceSubtotal;
            model.VatAmount = referenceVat;
            model.TotalAmount = referenceTotal;
            model.PaidAmount = 0m;
            model.BalanceAmount = referenceTotal;
            EnsureAtLeastOneLine(model);
            return false;
        }

        var dueAmount = model.QuotationId.HasValue && model.AmountDueThisInvoice.HasValue
            ? model.AmountDueThisInvoice.Value
            : referenceTotal;

        if (model.QuotationId.HasValue && model.AmountDueThisInvoice.HasValue && referenceTotal > 0)
        {
            var ratio = dueAmount / referenceTotal;
            var computedSubtotal = Math.Round(referenceSubtotal * ratio, 2, MidpointRounding.AwayFromZero);
            var computedDiscount = Math.Round(referenceAppliedDiscount * ratio, 2, MidpointRounding.AwayFromZero);
            var computedVat = Math.Round(referenceVat * ratio, 2, MidpointRounding.AwayFromZero);
            var computedTotal = computedSubtotal - computedDiscount + computedVat;
            var roundingDifference = dueAmount - computedTotal;

            model.Subtotal = computedSubtotal;
            model.DiscountAmount = useLineDiscount ? computedDiscount : 0m;
            model.HeaderDiscountAmount = useLineDiscount ? 0m : computedDiscount;
            model.VatAmount = computedVat + roundingDifference;
            model.TotalAmount = dueAmount;
        }
        else
        {
            model.Subtotal = referenceSubtotal;
            model.DiscountAmount = useLineDiscount ? referenceDiscount : 0m;
            model.VatAmount = referenceVat;
            model.TotalAmount = referenceTotal;
            model.AmountDueThisInvoice = referenceTotal;
        }

        model.PaidAmount = 0m;
        model.BalanceAmount = model.TotalAmount;
        EnsureAtLeastOneLine(model);
        return ModelState.IsValid;
    }

    private static decimal CalculateReferenceSubtotal(IEnumerable<InvoiceLineEditorViewModel> details)
    {
        return details.Sum(x => x.QuotedQty * x.UnitPrice);
    }

    private static decimal CalculateReferenceLineDiscount(IEnumerable<InvoiceLineEditorViewModel> details)
    {
        return details.Sum(x => x.DiscountAmount);
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
                x.QuotedQty > 0 ||
                x.Qty > 0 ||
                x.UnitPrice > 0 ||
                x.DiscountAmount > 0 ||
                x.SelectedSerialIds.Count > 0 ||
                !string.IsNullOrWhiteSpace(x.Remark))
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                x.QuotedQty = x.QuotedQty > 0 ? x.QuotedQty : x.Qty;
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
            QuotationDetailId = detail.QuotationDetailId,
            QuotedQty = detail.QuotedQty,
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
