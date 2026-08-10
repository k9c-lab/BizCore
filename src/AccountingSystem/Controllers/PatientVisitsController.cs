using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class PatientVisitsController : CrudControllerBase
{
    private const string VnPrefix = "VN";
    private const string InvPrefix = "INV";
    private readonly AccountingDbContext _context;

    public PatientVisitsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 20)
    {
        var query = _context.PatientVisits
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.TreatmentRight)
            .Include(x => x.Invoice)
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
                x.VN.Contains(keyword) ||
                (x.Patient != null && (
                    x.Patient.HN.Contains(keyword) ||
                    x.Patient.FirstName.Contains(keyword) ||
                    x.Patient.LastName.Contains(keyword))) ||
                (x.ReferringHospital != null && x.ReferringHospital.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        if (dateFrom.HasValue)
            query = query.Where(x => x.VisitDate >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            query = query.Where(x => x.VisitDate <= dateTo.Value.Date);

        ViewData["Search"] = search;
        ViewData["Status"] = status;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");

        return View(await PaginatedList<PatientVisit>.CreateAsync(
            query.OrderByDescending(x => x.VisitDate).ThenByDescending(x => x.PatientVisitId),
            page, pageSize));
    }

    public async Task<IActionResult> Create()
    {
        var model = new PatientVisitFormViewModel
        {
            VN = await GetNextVNAsync(DateTime.Today),
            BranchId = CurrentBranchId(),
            BranchName = await GetBranchNameAsync(CurrentBranchId())
        };
        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientVisitFormViewModel model)
    {
        if (!model.PatientId.HasValue)
            ModelState.AddModelError(nameof(model.PatientId), "กรุณาเลือกคนไข้");

        var validItems = model.Items.Where(x => x.ItemId.HasValue).ToList();

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        model.VN = await GetNextVNAsync(model.VisitDate);

        var visit = new PatientVisit
        {
            VN = model.VN,
            VisitDate = model.VisitDate,
            PatientId = model.PatientId!.Value,
            CustomerId = model.CustomerId,
            Weight = model.Weight,
            Height = model.Height,
            Ward = model.Ward,
            TreatmentRightId = model.TreatmentRightId,
            ReferringDoctorId = model.ReferringDoctorId,
            ReferringHospital = model.ReferringHospital,
            Remark = model.Remark,
            Status = "Registered",
            BranchId = model.BranchId,
            CreatedByUserId = CurrentUserId(),
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in validItems)
        {
            visit.PatientVisitItems.Add(new PatientVisitItem
            {
                ItemId = item.ItemId!.Value,
                Quantity = item.Quantity
            });
        }

        _context.PatientVisits.Add(visit);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "ไม่สามารถบันทึกได้ กรุณาลองใหม่");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = visit.PatientVisitId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var visit = await _context.PatientVisits
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Customer)
            .Include(x => x.TreatmentRight)
            .Include(x => x.ReferringDoctor)
            .Include(x => x.Invoice)
            .Include(x => x.Branch)
            .Include(x => x.PatientVisitItems)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.PatientVisitId == id.Value);

        if (visit is null || !CanAccessBranch(visit.BranchId))
            return NotFound();

        var customerLookup = await _context.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CustomerCode)
            .Select(x => new QuotationCustomerLookupViewModel
            {
                CustomerId = x.CustomerId,
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                TaxId = x.TaxId ?? string.Empty,
                Phone = x.PhoneNumber ?? string.Empty
            })
            .ToListAsync();

        ViewData["CustomerLookup"] = System.Text.Json.JsonSerializer.Serialize(customerLookup);

        return View(visit);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvoice(int id, int? customerId)
    {
        var visit = await _context.PatientVisits
            .Include(x => x.Patient)
            .Include(x => x.PatientVisitItems)
                .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.PatientVisitId == id);

        if (visit is null || !CanAccessBranch(visit.BranchId))
            return NotFound();

        if (visit.Status != "Registered")
        {
            TempData["VisitError"] = "ไม่สามารถสร้างใบแจ้งหนี้ได้ สถานะต้องเป็น Registered";
            return RedirectToAction(nameof(Details), new { id });
        }

        var payerId = customerId ?? visit.CustomerId;
        if (!payerId.HasValue)
        {
            TempData["VisitError"] = "กรุณาระบุลูกค้า (ผู้ชำระเงิน) ก่อนสร้างใบแจ้งหนี้";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!visit.PatientVisitItems.Any())
        {
            TempData["VisitError"] = "กรุณาเพิ่มรายการตรวจอย่างน้อย 1 รายการก่อนสร้างใบแจ้งหนี้";
            return RedirectToAction(nameof(Details), new { id });
        }

        var invoiceDate = visit.VisitDate;
        var invoiceNo = await GetNextInvoiceNumberAsync(invoiceDate);
        var patient = visit.Patient!;

        var invoice = new InvoiceHeader
        {
            InvoiceNo = invoiceNo,
            InvoiceDate = invoiceDate,
            CustomerId = payerId.Value,
            BranchId = visit.BranchId,
            Status = "Draft",
            VatType = "NoVAT",
            PatientFullName = $"{patient.FirstName} {patient.LastName}",
            PatientHn = patient.HN,
            PatientGender = patient.Gender,
            PatientBirthDate = patient.DateOfBirth,
            TreatmentRightId = visit.TreatmentRightId,
            PatientWard = visit.Ward,
            ReferringDoctorId = visit.ReferringDoctorId,
            Remark = visit.Remark,
            CreatedByUserId = CurrentUserId(),
            CreatedDate = DateTime.UtcNow
        };

        var lineNumber = 1;
        foreach (var visitItem in visit.PatientVisitItems)
        {
            var unitPrice = visitItem.Item!.UnitPrice;
            var lineTotal = unitPrice * visitItem.Quantity;
            invoice.InvoiceDetails.Add(new InvoiceDetail
            {
                LineNumber = lineNumber++,
                ItemId = visitItem.ItemId,
                Qty = visitItem.Quantity,
                QuotedQty = visitItem.Quantity,
                UnitPrice = unitPrice,
                DiscountAmount = 0,
                LineTotal = lineTotal
            });
        }

        invoice.Subtotal = invoice.InvoiceDetails.Sum(x => x.LineTotal);
        invoice.DiscountAmount = 0;
        invoice.VatAmount = 0;
        invoice.TotalAmount = invoice.Subtotal;
        invoice.BalanceAmount = invoice.TotalAmount;

        _context.InvoiceHeaders.Add(invoice);
        await _context.SaveChangesAsync();

        visit.Status = "Invoiced";
        visit.InvoiceId = invoice.InvoiceId;
        if (customerId.HasValue && !visit.CustomerId.HasValue)
            visit.CustomerId = customerId;

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Invoices", new { id = invoice.InvoiceId });
    }

    [HttpGet]
    public async Task<IActionResult> NextVN(string? date)
    {
        var parsedDate = DateTime.TryParse(date, out var d) ? d : DateTime.Today;
        var vn = await GetNextVNAsync(parsedDate);
        return Json(new { vn });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var visit = await _context.PatientVisits.FindAsync(id);
        if (visit is null || !CanAccessBranch(visit.BranchId))
            return NotFound();

        if (visit.Status != "Registered")
        {
            TempData["VisitError"] = "ไม่สามารถยกเลิกได้ สถานะต้องเป็น Registered";
            return RedirectToAction(nameof(Details), new { id });
        }

        visit.Status = "Cancelled";
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    private bool CanAccessBranch(int? branchId) =>
        CurrentUserCanAccessAllBranches() || branchId == CurrentBranchId();

    private async Task<string> GetBranchNameAsync(int? branchId)
    {
        if (!branchId.HasValue) return string.Empty;
        var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.BranchId == branchId.Value);
        return branch?.BranchName ?? string.Empty;
    }

    private async Task<string> GetNextVNAsync(DateTime date)
    {
        var prefix = $"{VnPrefix}-{date:yyyyMM}-";
        var codes = await _context.PatientVisits
            .Where(x => x.VN.StartsWith(prefix))
            .Select(x => x.VN)
            .ToListAsync();
        var nextSeq = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(VnPrefix, date, nextSeq);
    }

    private async Task<string> GetNextInvoiceNumberAsync(DateTime date)
    {
        var prefix = $"{InvPrefix}-{date:yyyyMM}-";
        var codes = await _context.InvoiceHeaders
            .Where(x => x.InvoiceNo.StartsWith(prefix))
            .Select(x => x.InvoiceNo)
            .ToListAsync();
        var nextSeq = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPeriodPrefixedCode(InvPrefix, date, nextSeq);
    }

    private async Task PopulateLookupsAsync(PatientVisitFormViewModel model)
    {
        model.TreatmentRightOptions = await _context.TreatmentRights
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.TreatmentRightCode)
            .Select(x => new SelectListItem(
                $"{x.TreatmentRightCode} - {x.TreatmentRightName}",
                x.TreatmentRightId.ToString()))
            .ToListAsync();

        model.ReferringDoctorOptions = await _context.ReferringDoctors
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DoctorCode)
            .Select(x => new SelectListItem(
                $"{x.DoctorCode} - {x.DoctorName}",
                x.ReferringDoctorId.ToString()))
            .ToListAsync();

        model.ItemLookup = await _context.Items
            .AsNoTracking()
            .Where(x => x.IsActive && x.ItemType == "Service")
            .OrderBy(x => x.ItemCode)
            .Select(x => new PatientVisitItemLookupViewModel
            {
                ItemId = x.ItemId,
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                Unit = x.Unit,
                UnitPrice = x.UnitPrice
            })
            .ToListAsync();

        model.PatientLookup = await _context.Patients
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.HN)
            .Select(x => new PatientLookupViewModel
            {
                PatientId = x.PatientId,
                HN = x.HN,
                FullName = x.FirstName + " " + x.LastName,
                NationalId = x.NationalId,
                Phone = x.Phone,
                Gender = x.Gender,
                DateOfBirth = x.DateOfBirth != null ? x.DateOfBirth.Value.ToString("yyyy-MM-dd") : null
            })
            .ToListAsync();

        model.CustomerLookup = await _context.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CustomerCode)
            .Select(x => new QuotationCustomerLookupViewModel
            {
                CustomerId = x.CustomerId,
                CustomerCode = x.CustomerCode,
                CustomerName = x.CustomerName,
                TaxId = x.TaxId ?? string.Empty,
                Phone = x.PhoneNumber ?? string.Empty
            })
            .ToListAsync();

        if (model.BranchId.HasValue && string.IsNullOrEmpty(model.BranchName))
            model.BranchName = await GetBranchNameAsync(model.BranchId);
    }
}
