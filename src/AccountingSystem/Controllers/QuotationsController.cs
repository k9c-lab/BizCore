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
public class QuotationsController : CrudControllerBase
{
    private const string NumberPrefix = "QT";
    private const string InvoiceNumberPrefix = "INV";
    private readonly AccountingDbContext _context;
    private readonly CompanyProfileSettings _companyProfile;

    public QuotationsController(AccountingDbContext context, IOptions<CompanyProfileSettings> companyProfileOptions)
    {
        _context = context;
        _companyProfile = companyProfileOptions.Value;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.QuotationHeaders
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
                x.QuotationNumber.Contains(keyword) ||
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
            query = query.Where(x => x.QuotationDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var endDate = dateTo.Value.Date.AddDays(1);
            query = query.Where(x => x.QuotationDate < endDate);
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        var quotations = await PaginatedList<QuotationHeader>.CreateAsync(query
            .OrderByDescending(x => x.QuotationDate)
            .ThenByDescending(x => x.QuotationHeaderId), page, pageSize);

        ViewData["PaymentStatusMap"] = await BuildQuotationPaymentStatusMapAsync(
            quotations.Select(x => (x.QuotationHeaderId, x.TotalAmount)));

        return View(quotations);
    }

    public async Task<IActionResult> Create()
    {
        var model = new QuotationFormViewModel
        {
            QuotationNumber = await GetNextQuotationNumberAsync(DateTime.Today),
            Status = "Draft",
            DiscountMode = "Line",
            VatType = "NoVAT",
            ExpiryDate = null,
            BranchId = CurrentBranchId()
        };

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> BranchStock(int? branchId)
    {
        var effectiveBranchId = CurrentUserCanAccessAllBranches() ? branchId : CurrentBranchId();
        if (!effectiveBranchId.HasValue)
        {
            return Json(Array.Empty<object>());
        }

        if (!CanAccessBranch(effectiveBranchId) ||
            !await _context.Branches.AnyAsync(x => x.BranchId == effectiveBranchId.Value && x.IsActive))
        {
            return BadRequest();
        }

        var stock = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.BranchId == effectiveBranchId.Value)
            .GroupBy(x => x.ItemId)
            .Select(x => new
            {
                itemId = x.Key,
                currentStock = x.Sum(b => b.QtyOnHand)
            })
            .ToListAsync();

        return Json(stock);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuotationFormViewModel model)
    {
        model.QuotationNumber = await GetNextQuotationNumberAsync(model.QuotationDate);
        ModelState.Remove(nameof(QuotationFormViewModel.QuotationNumber));

        if (!await ValidateAndComputeAsync(model))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var header = new QuotationHeader
        {
            QuotationNumber = model.QuotationNumber,
            QuotationDate = model.QuotationDate.Date,
            ExpiryDate = model.ExpiryDate?.Date,
            CustomerId = model.CustomerId!.Value,
            SalespersonId = model.SalespersonId,
            BranchId = model.BranchId,
            ReferenceNo = model.ReferenceNo?.Trim(),
            Status = model.Status,
            Remarks = model.Remarks?.Trim(),
            Subtotal = model.Subtotal,
            DiscountAmount = model.DiscountAmount,
            DiscountMode = model.DiscountMode,
            HeaderDiscountAmount = model.HeaderDiscountAmount,
            HeaderDiscountType = model.HeaderDiscountType,
            HeaderDiscountPercent = model.HeaderDiscountPercent,
            VatType = model.VatType,
            VatAmount = model.VatAmount,
            TotalAmount = model.TotalAmount,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = CurrentUserId(),
            QuotationDetails = model.Details.Select(MapDetailEntity).ToList()
        };

        _context.QuotationHeaders.Add(header);

        if (!await TrySaveAsync("Quotation number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = header.QuotationHeaderId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var header = await _context.QuotationHeaders
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.QuotationDetails)
            .FirstOrDefaultAsync(x => x.QuotationHeaderId == id.Value);

        if (header is null || !CanAccessBranch(header.BranchId))
        {
            return NotFound();
        }

        if (header.Status != "Draft")
        {
            TempData["QuotationNotice"] = "Only draft quotations can be edited.";
            return RedirectToAction(nameof(Details), new { id = header.QuotationHeaderId });
        }

        var model = new QuotationFormViewModel
        {
            QuotationHeaderId = header.QuotationHeaderId,
            QuotationNumber = header.QuotationNumber,
            QuotationDate = header.QuotationDate,
            ExpiryDate = header.ExpiryDate,
            CustomerId = header.CustomerId,
            SalespersonId = header.SalespersonId,
            BranchId = header.BranchId,
            BranchName = header.Branch?.BranchName ?? string.Empty,
            ReferenceNo = header.ReferenceNo,
            Status = header.Status,
            VatType = header.VatType,
            Remarks = header.Remarks,
            Subtotal = header.Subtotal,
            DiscountAmount = header.DiscountAmount,
            DiscountMode = header.DiscountMode,
            HeaderDiscountAmount = header.HeaderDiscountAmount,
            HeaderDiscountType = header.HeaderDiscountType,
            HeaderDiscountPercent = header.HeaderDiscountPercent,
            VatAmount = header.VatAmount,
            TotalAmount = header.TotalAmount,
            Details = header.QuotationDetails
                .OrderBy(x => x.LineNumber)
                .Select(x => new QuotationLineEditorViewModel
                {
                    QuotationDetailId = x.QuotationDetailId,
                    LineNumber = x.LineNumber,
                    ItemId = x.ItemId,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    DiscountAmount = x.DiscountAmount,
                    DiscountType = x.DiscountType,
                    DiscountPercent = x.DiscountPercent,
                    LineTotal = x.LineTotal
                })
                .ToList()
        };

        if (model.Details.Count == 0)
        {
            model.Details.Add(new QuotationLineEditorViewModel { LineNumber = 1 });
        }

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, QuotationFormViewModel model)
    {
        if (id != model.QuotationHeaderId)
        {
            return NotFound();
        }

        if (!await ValidateAndComputeAsync(model))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var header = await _context.QuotationHeaders
            .Include(x => x.QuotationDetails)
            .FirstOrDefaultAsync(x => x.QuotationHeaderId == id);

        if (header is null || !CanAccessBranch(header.BranchId))
        {
            return NotFound();
        }

        if (header.Status != "Draft")
        {
            TempData["QuotationNotice"] = "Only draft quotations can be edited.";
            return RedirectToAction(nameof(Details), new { id = header.QuotationHeaderId });
        }

        header.QuotationNumber = model.QuotationNumber.Trim();
        header.QuotationDate = model.QuotationDate.Date;
        header.ExpiryDate = model.ExpiryDate?.Date;
        header.CustomerId = model.CustomerId!.Value;
        header.SalespersonId = model.SalespersonId;
        header.BranchId = model.BranchId;
        header.ReferenceNo = model.ReferenceNo?.Trim();
        header.Status = model.Status;
        header.VatType = model.VatType;
        header.Remarks = model.Remarks?.Trim();
        header.Subtotal = model.Subtotal;
        header.DiscountAmount = model.DiscountAmount;
        header.DiscountMode = model.DiscountMode;
        header.HeaderDiscountAmount = model.HeaderDiscountAmount;
        header.HeaderDiscountType = model.HeaderDiscountType;
        header.HeaderDiscountPercent = model.HeaderDiscountPercent;
        header.VatAmount = model.VatAmount;
        header.TotalAmount = model.TotalAmount;
        header.UpdatedDate = DateTime.UtcNow;
        header.UpdatedByUserId = CurrentUserId();

        _context.QuotationDetails.RemoveRange(header.QuotationDetails);
        header.QuotationDetails = model.Details.Select(MapDetailEntity).ToList();

        if (!await TrySaveAsync("Quotation number must be unique."))
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = header.QuotationHeaderId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var quotation = await _context.QuotationHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Branch)
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.ConvertedByUser)
            .Include(x => x.QuotationDetails)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.QuotationHeaderId == id.Value);

        if (quotation is null || !CanAccessBranch(quotation.BranchId))
        {
            return NotFound();
        }

        ViewData["PaymentStatus"] = (await BuildQuotationPaymentStatusMapAsync(new[] { (quotation.QuotationHeaderId, quotation.TotalAmount) }))
            .GetValueOrDefault(quotation.QuotationHeaderId, new QuotationPaymentStatusViewModel
            {
                QuotationHeaderId = quotation.QuotationHeaderId,
                QuotationTotalAmount = quotation.TotalAmount,
                RemainingAmount = quotation.TotalAmount,
                Status = "Not Invoiced"
            });

        return View(quotation);
    }

    public async Task<IActionResult> Print(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var quotation = await _context.QuotationHeaders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Salesperson)
            .Include(x => x.Branch)
            .Include(x => x.QuotationDetails)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.QuotationHeaderId == id.Value);

        if (quotation is null || !CanAccessBranch(quotation.BranchId))
        {
            return NotFound();
        }

        PopulatePrintCompanyViewData(_companyProfile);
        return View(quotation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToInvoice(int id)
    {
        var quotation = await _context.QuotationHeaders
            .FirstOrDefaultAsync(x => x.QuotationHeaderId == id);

        if (quotation is null || !CanAccessBranch(quotation.BranchId))
        {
            return NotFound();
        }

        if (quotation.Status is not ("Approved" or "Converted"))
        {
            TempData["QuotationNotice"] = "Only approved quotations can be used to prefill an invoice.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction("Create", "Invoices", new { quotationId = quotation.QuotationHeaderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var quotation = await _context.QuotationHeaders
            .FirstOrDefaultAsync(x => x.QuotationHeaderId == id);

        if (quotation is null || !CanAccessBranch(quotation.BranchId))
        {
            return NotFound();
        }

        if (quotation.Status != "Draft")
        {
            TempData["QuotationNotice"] = "Only draft quotations can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        quotation.Status = "Approved";
        quotation.UpdatedDate = DateTime.UtcNow;
        quotation.UpdatedByUserId = CurrentUserId();
        quotation.ApprovedByUserId = CurrentUserId();
        quotation.ApprovedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["QuotationNotice"] = "Quotation approved successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateLookupsAsync(QuotationFormViewModel model)
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

        var selectedBranchId = model.BranchId;
        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .OrderBy(x => x.ItemCode)
            .Select(x => new QuotationItemLookupViewModel
            {
                ItemId = x.ItemId,
                DisplayText = $"{x.ItemCode} - {x.ItemName} - {x.PartNumber}",
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                PartNumber = x.PartNumber,
                ItemType = x.ItemType,
                UnitPrice = x.UnitPrice,
                CurrentStock = selectedBranchId.HasValue
                    ? (_context.StockBalances
                        .Where(b => b.ItemId == x.ItemId && b.BranchId == selectedBranchId.Value)
                        .Sum(b => (decimal?)b.QtyOnHand) ?? 0m)
                    : 0m,
                TrackStock = x.TrackStock,
                IsSerialControlled = x.IsSerialControlled
            })
            .ToListAsync();

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

        model.StatusOptions = new[]
        {
            new SelectListItem("Draft", "Draft"),
            new SelectListItem("Approved", "Approved"),
            new SelectListItem("Cancelled", "Cancelled")
        };

        model.DiscountModeOptions = new[]
        {
            new SelectListItem("Line", "Line"),
            new SelectListItem("Header", "Header")
        };

        model.DiscountTypeOptions = new[]
        {
            new SelectListItem("Amount", "Amount"),
            new SelectListItem("Percent", "Percent")
        };

        model.VatTypeOptions = new[]
        {
            new SelectListItem("VAT", "VAT"),
            new SelectListItem("No VAT", "NoVAT")
        };
    }

    private async Task<bool> ValidateAndComputeAsync(QuotationFormViewModel model)
    {
        model.Details = NormalizeDetails(model.Details);

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Details), "Please add at least one quotation line.");
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
            ModelState.AddModelError(nameof(model.BranchId), "You cannot create or edit quotations for this branch.");
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

        if (model.HeaderDiscountType is not ("Amount" or "Percent"))
        {
            ModelState.AddModelError(nameof(model.HeaderDiscountType), "Header discount type must be Amount or Percent.");
        }

        var itemIds = model.Details.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToList();
        var itemMap = await _context.Items
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => x.ItemId);

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

            detail.Description = string.IsNullOrWhiteSpace(detail.Description)
                ? item.ItemName
                : detail.Description.Trim();

            if (detail.UnitPrice <= 0)
            {
                detail.UnitPrice = item.UnitPrice;
            }

            var gross = detail.Quantity * detail.UnitPrice;
            if (!useLineDiscount)
            {
                detail.DiscountAmount = 0m;
                detail.DiscountPercent = 0m;
            }

            if (detail.DiscountType is not ("Amount" or "Percent"))
            {
                ModelState.AddModelError($"Details[{i}].DiscountType", "Discount type must be Amount or Percent.");
                continue;
            }

            if (detail.DiscountPercent < 0 || detail.DiscountPercent > 100)
            {
                ModelState.AddModelError($"Details[{i}].DiscountPercent", "Discount percent must be between 0 and 100.");
                continue;
            }

            if (useLineDiscount && detail.DiscountType == "Percent")
            {
                detail.DiscountAmount = Math.Round(gross * (detail.DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
            }
            else if (useLineDiscount && detail.DiscountType == "Amount")
            {
                detail.DiscountPercent = gross <= 0
                    ? 0m
                    : Math.Round((detail.DiscountAmount / gross) * 100m, 4, MidpointRounding.AwayFromZero);
            }

            if (detail.DiscountAmount > gross)
            {
                ModelState.AddModelError($"Details[{i}].DiscountAmount", "Discount cannot exceed the line amount.");
                continue;
            }

            detail.LineTotal = gross - detail.DiscountAmount;
            if (useLineDiscount)
            {
                subtotal += detail.LineTotal;
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
            model.HeaderDiscountPercent = 0m;
        }

        if (!useLineDiscount && model.HeaderDiscountPercent > 100)
        {
            ModelState.AddModelError(nameof(model.HeaderDiscountPercent), "Header discount percent must be between 0 and 100.");
        }

        if (!useLineDiscount && model.HeaderDiscountType == "Percent")
        {
            model.HeaderDiscountAmount = Math.Round(subtotal * (model.HeaderDiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
        }
        else if (!useLineDiscount && model.HeaderDiscountType == "Amount")
        {
            model.HeaderDiscountPercent = subtotal <= 0
                ? 0m
                : Math.Round((model.HeaderDiscountAmount / subtotal) * 100m, 4, MidpointRounding.AwayFromZero);
        }

        if (!useLineDiscount && model.HeaderDiscountAmount > subtotal)
        {
            ModelState.AddModelError(nameof(model.HeaderDiscountAmount), "Header discount cannot exceed subtotal.");
        }

        model.Subtotal = subtotal;
        model.DiscountAmount = discount;

        var netBeforeVat = useLineDiscount
            ? subtotal
            : subtotal - model.HeaderDiscountAmount;

        model.VatAmount = model.VatType == "VAT"
            ? Math.Round(Math.Max(netBeforeVat, 0m) * 0.07m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        model.TotalAmount = netBeforeVat + model.VatAmount;
        EnsureAtLeastOneLine(model);
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

    private bool CanAccessBranch(int? branchId)
    {
        return CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();
    }

    private static void EnsureAtLeastOneLine(QuotationFormViewModel model)
    {
        if (model.Details.Count == 0)
        {
            model.Details.Add(new QuotationLineEditorViewModel { LineNumber = 1 });
        }
    }

    private static List<QuotationLineEditorViewModel> NormalizeDetails(IEnumerable<QuotationLineEditorViewModel>? details)
    {
        return (details ?? Enumerable.Empty<QuotationLineEditorViewModel>())
            .Where(x =>
                x.ItemId.HasValue ||
                !string.IsNullOrWhiteSpace(x.Description) ||
                x.Quantity > 0 ||
                x.UnitPrice > 0 ||
                x.DiscountAmount > 0 ||
                x.DiscountPercent > 0)
            .Select((x, index) =>
            {
                x.LineNumber = index + 1;
                return x;
            })
            .ToList();
    }

    private static QuotationDetail MapDetailEntity(QuotationLineEditorViewModel detail)
    {
        return new QuotationDetail
        {
            LineNumber = detail.LineNumber,
            ItemId = detail.ItemId!.Value,
            Description = detail.Description?.Trim(),
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice,
            DiscountType = detail.DiscountType,
            DiscountPercent = detail.DiscountPercent,
            DiscountAmount = detail.DiscountAmount,
            LineTotal = detail.LineTotal
        };
    }

    private async Task<IReadOnlyList<string>> GetBranchStockShortageMessagesAsync(QuotationHeader quotation)
    {
        if (!quotation.BranchId.HasValue)
        {
            return new[] { "quotation branch is missing" };
        }

        var requiredItems = quotation.QuotationDetails
            .Where(x => x.Item?.TrackStock == true)
            .GroupBy(x => new
            {
                x.ItemId,
                x.Item!.ItemCode,
                x.Item.ItemName
            })
            .Select(x => new
            {
                x.Key.ItemId,
                x.Key.ItemCode,
                x.Key.ItemName,
                RequiredQty = x.Sum(d => d.Quantity)
            })
            .ToList();

        if (requiredItems.Count == 0)
        {
            return Array.Empty<string>();
        }

        var itemIds = requiredItems.Select(x => x.ItemId).ToList();
        var stockMap = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.BranchId == quotation.BranchId.Value && itemIds.Contains(x.ItemId))
            .GroupBy(x => x.ItemId)
            .Select(x => new
            {
                ItemId = x.Key,
                Qty = x.Sum(b => b.QtyOnHand)
            })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty);

        return requiredItems
            .Where(x => !stockMap.TryGetValue(x.ItemId, out var availableQty) || availableQty < x.RequiredQty)
            .Select(x =>
            {
                var availableQty = stockMap.TryGetValue(x.ItemId, out var qty) ? qty : 0m;
                return $"{x.ItemCode} - {x.ItemName} needs {x.RequiredQty:N2}, available {availableQty:N2}";
            })
            .ToList();
    }

    private Task<string> GetNextQuotationNumberAsync(DateTime date)
    {
        var prefix = $"{NumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.QuotationHeaders.Select(x => x.QuotationNumber), prefix, NumberPrefix, date);
    }

    private Task<string> GetNextInvoiceNumberAsync(DateTime date)
    {
        var prefix = $"{InvoiceNumberPrefix}-{date:yyyyMM}-";
        return GetNextPeriodCodeAsync(_context.InvoiceHeaders.Select(x => x.InvoiceNo), prefix, InvoiceNumberPrefix, date);
    }

    private static async Task<string> GetNextPeriodCodeAsync(IQueryable<string> codesQuery, string prefix, string numberPrefix, DateTime date)
    {
        var codes = await codesQuery.Where(x => x.StartsWith(prefix)).ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(numberPrefix, date, nextSequence);
    }

    private async Task<Dictionary<int, QuotationPaymentStatusViewModel>> BuildQuotationPaymentStatusMapAsync(IEnumerable<(int QuotationHeaderId, decimal TotalAmount)> quotationRows)
    {
        var quotations = quotationRows
            .Distinct()
            .ToDictionary(x => x.QuotationHeaderId, x => x.TotalAmount);

        if (quotations.Count == 0)
        {
            return new Dictionary<int, QuotationPaymentStatusViewModel>();
        }

        var quotationIds = quotations.Keys.ToList();
        var invoiceSummaries = await _context.InvoiceHeaders
            .AsNoTracking()
            .Where(x => x.QuotationId.HasValue &&
                        quotationIds.Contains(x.QuotationId.Value) &&
                        x.Status != "Cancelled")
            .GroupBy(x => x.QuotationId!.Value)
            .Select(x => new
            {
                QuotationHeaderId = x.Key,
                InvoiceCount = x.Count(),
                InvoicedAmount = x.Sum(i => i.TotalAmount),
                PaidAmount = x.Sum(i => i.PaidAmount)
            })
            .ToDictionaryAsync(x => x.QuotationHeaderId);

        return quotations.ToDictionary(
            x => x.Key,
            x =>
            {
                var hasSummary = invoiceSummaries.TryGetValue(x.Key, out var summary);
                var paidAmount = hasSummary ? summary!.PaidAmount : 0m;
                var invoicedAmount = hasSummary ? summary!.InvoicedAmount : 0m;
                var remainingAmount = Math.Max(x.Value - invoicedAmount, 0m);
                var statusValue = invoicedAmount >= x.Value && x.Value > 0m
                    ? "Fully Invoiced"
                    : invoicedAmount > 0m
                        ? "Partially Invoiced"
                        : "Not Invoiced";

                return new QuotationPaymentStatusViewModel
                {
                    QuotationHeaderId = x.Key,
                    InvoiceCount = hasSummary ? summary!.InvoiceCount : 0,
                    QuotationTotalAmount = x.Value,
                    InvoicedAmount = invoicedAmount,
                    PaidAmount = paidAmount,
                    RemainingAmount = remainingAmount,
                    Status = statusValue
                };
            });
    }
}
