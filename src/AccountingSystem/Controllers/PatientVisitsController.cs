using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Utilities;
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
        if (model.IsNewPatient)
        {
            ModelState.Remove(nameof(model.PatientId));
            if (string.IsNullOrWhiteSpace(model.NewPatientHN))
                ModelState.AddModelError(nameof(model.NewPatientHN), "กรุณากรอก HN");
            if (string.IsNullOrWhiteSpace(model.NewPatientFirstName))
                ModelState.AddModelError(nameof(model.NewPatientFirstName), "กรุณากรอกชื่อ");
            if (string.IsNullOrWhiteSpace(model.NewPatientLastName))
                ModelState.AddModelError(nameof(model.NewPatientLastName), "กรุณากรอกนามสกุล");
        }
        else
        {
            if (!model.PatientId.HasValue)
                ModelState.AddModelError(nameof(model.PatientId), "กรุณาเลือกคนไข้");
        }

        var validItems = model.Items.Where(x => x.ItemId.HasValue).ToList();

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        if (model.IsNewPatient)
        {
            var newPatient = new Patient
            {
                HN = model.NewPatientHN!.Trim(),
                NationalId = model.NewPatientNationalId?.Trim(),
                NamePrefix = model.NewPatientNamePrefix?.Trim(),
                FirstName = model.NewPatientFirstName!.Trim(),
                LastName = model.NewPatientLastName!.Trim(),
                Gender = model.NewPatientGender ?? string.Empty,
                DateOfBirth = model.NewPatientDateOfBirth,
                Phone = model.NewPatientPhone?.Trim(),
                IsActive = true
            };
            _context.Patients.Add(newPatient);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
            {
                ModelState.AddModelError(nameof(model.NewPatientHN), "HN นี้มีอยู่ในระบบแล้ว กรุณาตรวจสอบ");
                await PopulateLookupsAsync(model);
                return View(model);
            }
            model.PatientId = newPatient.PatientId;
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

        if (!model.IsNewPatient && model.PatientId.HasValue && !string.IsNullOrWhiteSpace(model.UpdatePatientPrefix))
        {
            var patient = await _context.Patients.FindAsync(model.PatientId.Value);
            if (patient is not null && string.IsNullOrWhiteSpace(patient.NamePrefix))
            {
                patient.NamePrefix = model.UpdatePatientPrefix.Trim();
                await _context.SaveChangesAsync();
            }
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
            .Include(x => x.AuditLogs.OrderByDescending(a => a.OccurredAt))
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
            VatType = VatModeHelper.VatInclusive,
            PatientFullName = string.IsNullOrWhiteSpace(patient.NamePrefix)
                ? $"{patient.FirstName} {patient.LastName}"
                : $"{patient.NamePrefix} {patient.FirstName} {patient.LastName}",
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
        var vatComp = VatModeHelper.ComputeFromDocumentPricing(invoice.Subtotal, VatModeHelper.VatInclusive);
        invoice.VatAmount = vatComp.VatAmount;
        invoice.TotalAmount = vatComp.TotalAmount;
        invoice.BalanceAmount = vatComp.TotalAmount;

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
                NamePrefix = x.NamePrefix,
                FullName = (x.NamePrefix != null ? x.NamePrefix + " " : "") + x.FirstName + " " + x.LastName,
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

    private void AddVisitAuditLog(int patientVisitId, string description)
    {
        _context.PatientVisitAuditLogs.Add(new PatientVisitAuditLog
        {
            PatientVisitId = patientVisitId,
            UserId = CurrentUserId(),
            UserName = User.Identity?.Name ?? "",
            OccurredAt = DateTime.UtcNow,
            Description = description
        });
    }

    private PatientVisitFormViewModel MapVisitToEditModel(PatientVisit visit)
    {
        var patient = visit.Patient;
        return new PatientVisitFormViewModel
        {
            PatientVisitId = visit.PatientVisitId,
            VN = visit.VN,
            VisitDate = visit.VisitDate,
            PatientId = visit.PatientId,
            CustomerId = visit.CustomerId,
            Weight = visit.Weight,
            Height = visit.Height,
            Ward = visit.Ward,
            TreatmentRightId = visit.TreatmentRightId,
            ReferringDoctorId = visit.ReferringDoctorId,
            ReferringHospital = visit.ReferringHospital,
            Remark = visit.Remark,
            Status = visit.Status,
            BranchId = visit.BranchId,
            InvoiceId = visit.InvoiceId,
            InvoiceNo = visit.Invoice?.InvoiceNo,
            InvoiceStatus = visit.Invoice?.Status,
            EditPatientHN = patient?.HN,
            EditPatientNamePrefix = patient?.NamePrefix,
            EditPatientFirstName = patient?.FirstName,
            EditPatientLastName = patient?.LastName,
            EditPatientDateOfBirth = patient?.DateOfBirth,
            EditPatientGender = patient?.Gender,
            EditPatientPhone = patient?.Phone,
            Items = visit.PatientVisitItems.Select(i => new PatientVisitItemViewModel
            {
                PatientVisitItemId = i.PatientVisitItemId,
                ItemId = i.ItemId,
                ItemCode = i.Item?.ItemCode ?? "",
                ItemName = i.Item?.ItemName ?? "",
                Unit = i.Item?.Unit ?? "",
                Quantity = i.Quantity
            }).ToList(),
            AuditLogs = visit.AuditLogs
                .OrderByDescending(a => a.OccurredAt)
                .Select(a => new PatientVisitAuditLogViewModel
                {
                    UserName = a.UserName,
                    OccurredAt = a.OccurredAt,
                    Description = a.Description
                })
                .ToList()
        };
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var visit = await _context.PatientVisits
            .Include(x => x.Patient)
            .Include(x => x.PatientVisitItems)
                .ThenInclude(x => x.Item)
            .Include(x => x.Invoice)
            .Include(x => x.AuditLogs.OrderByDescending(a => a.OccurredAt))
            .FirstOrDefaultAsync(x => x.PatientVisitId == id.Value);

        if (visit is null || !CanAccessBranch(visit.BranchId))
            return NotFound();

        var model = MapVisitToEditModel(visit);
        model.BranchName = await GetBranchNameAsync(visit.BranchId);
        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PatientVisitFormViewModel model)
    {
        var visit = await _context.PatientVisits
            .Include(x => x.Patient)
            .Include(x => x.PatientVisitItems)
                .ThenInclude(x => x.Item)
            .Include(x => x.Invoice)
                .ThenInclude(x => x!.InvoiceDetails)
            .Include(x => x.AuditLogs.OrderByDescending(a => a.OccurredAt))
            .FirstOrDefaultAsync(x => x.PatientVisitId == id);

        if (visit is null || !CanAccessBranch(visit.BranchId))
            return NotFound();

        ModelState.Remove(nameof(model.PatientId));
        ModelState.Remove(nameof(model.VN));
        ModelState.Remove(nameof(model.IsNewPatient));

        var validItems = model.Items.Where(x => x.ItemId.HasValue).ToList();

        if (!ModelState.IsValid)
        {
            var errorModel = MapVisitToEditModel(visit);
            errorModel.BranchName = await GetBranchNameAsync(visit.BranchId);
            // keep user's input for editable fields
            errorModel.EditPatientNamePrefix = model.EditPatientNamePrefix;
            errorModel.EditPatientFirstName = model.EditPatientFirstName;
            errorModel.EditPatientLastName = model.EditPatientLastName;
            errorModel.EditPatientDateOfBirth = model.EditPatientDateOfBirth;
            errorModel.EditPatientGender = model.EditPatientGender;
            errorModel.EditPatientPhone = model.EditPatientPhone;
            errorModel.CustomerId = model.CustomerId;
            errorModel.ReferringHospital = model.ReferringHospital;
            errorModel.ReferringDoctorId = model.ReferringDoctorId;
            errorModel.TreatmentRightId = model.TreatmentRightId;
            errorModel.Weight = model.Weight;
            errorModel.Height = model.Height;
            errorModel.Ward = model.Ward;
            errorModel.Remark = model.Remark;
            errorModel.Items = validItems;
            await PopulateLookupsAsync(errorModel);
            return View(errorModel);
        }

        var auditLines = new List<string>();

        // --- Patient master changes ---
        var patient = visit.Patient;
        if (patient != null && model.EditPatientFirstName != null)
        {
            var changes = new List<string>();
            var newPrefix = model.EditPatientNamePrefix?.Trim();
            var newFirst = model.EditPatientFirstName.Trim();
            var newLast = model.EditPatientLastName?.Trim() ?? "";

            if (patient.NamePrefix != newPrefix)
            {
                changes.Add($"- คำนำหน้า: '{patient.NamePrefix ?? "-"}' → '{newPrefix ?? "-"}'");
                patient.NamePrefix = newPrefix;
            }
            if (patient.FirstName != newFirst || patient.LastName != newLast)
            {
                changes.Add($"- ชื่อ: '{patient.FirstName} {patient.LastName}' → '{newFirst} {newLast}'");
                patient.FirstName = newFirst;
                patient.LastName = newLast;
            }
            if (patient.DateOfBirth != model.EditPatientDateOfBirth)
            {
                static string FmtDob(DateTime? d) => d.HasValue
                    ? $"{d.Value:dd/MM}/{d.Value.Year + 543}" : "-";
                changes.Add($"- วันเกิด: '{FmtDob(patient.DateOfBirth)}' → '{FmtDob(model.EditPatientDateOfBirth)}'");
                patient.DateOfBirth = model.EditPatientDateOfBirth;
            }
            var newGender = model.EditPatientGender ?? patient.Gender;
            if (patient.Gender != newGender)
            {
                static string GL(string? g) => g == "M" ? "ชาย" : g == "F" ? "หญิง" : "-";
                changes.Add($"- เพศ: '{GL(patient.Gender)}' → '{GL(newGender)}'");
                patient.Gender = newGender;
            }
            var newPhone = model.EditPatientPhone?.Trim();
            if (patient.Phone != newPhone)
            {
                changes.Add($"- โทรศัพท์: '{patient.Phone ?? "-"}' → '{newPhone ?? "-"}'");
                patient.Phone = newPhone;
            }
            if (changes.Any())
                auditLines.Add("แก้ไขข้อมูลคนไข้:\n" + string.Join("\n", changes));
        }

        // --- Visit field changes ---
        {
            var changes = new List<string>();
            var newHosp = model.ReferringHospital?.Trim();
            if (visit.ReferringHospital != newHosp)
            {
                changes.Add($"- รพ.ส่งตัว: '{visit.ReferringHospital ?? "-"}' → '{newHosp ?? "-"}'");
                visit.ReferringHospital = newHosp;
            }

            if (visit.CustomerId != model.CustomerId)
            {
                var custIds = new[] { visit.CustomerId, model.CustomerId }
                    .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
                var custNames = custIds.Any()
                    ? (await _context.Customers.Where(x => custIds.Contains(x.CustomerId))
                        .Select(x => new { x.CustomerId, x.CustomerName }).ToListAsync())
                        .ToDictionary(x => x.CustomerId, x => x.CustomerName)
                    : new Dictionary<int, string>();
                var oldName = visit.CustomerId.HasValue && custNames.TryGetValue(visit.CustomerId.Value, out var on) ? on : "-";
                var newName = model.CustomerId.HasValue && custNames.TryGetValue(model.CustomerId.Value, out var nn) ? nn : "-";
                changes.Add($"- ผู้รับบิล: '{oldName}' → '{newName}'");
                visit.CustomerId = model.CustomerId;
            }

            if (visit.ReferringDoctorId != model.ReferringDoctorId)
            {
                var drIds = new[] { visit.ReferringDoctorId, model.ReferringDoctorId }
                    .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
                var drNames = drIds.Any()
                    ? (await _context.ReferringDoctors.Where(x => drIds.Contains(x.ReferringDoctorId))
                        .Select(x => new { x.ReferringDoctorId, x.DoctorName }).ToListAsync())
                        .ToDictionary(x => x.ReferringDoctorId, x => x.DoctorName)
                    : new Dictionary<int, string>();
                var oldDr = visit.ReferringDoctorId.HasValue && drNames.TryGetValue(visit.ReferringDoctorId.Value, out var od) ? od : "-";
                var newDr = model.ReferringDoctorId.HasValue && drNames.TryGetValue(model.ReferringDoctorId.Value, out var nd) ? nd : "-";
                changes.Add($"- แพทย์ส่ง: '{oldDr}' → '{newDr}'");
                visit.ReferringDoctorId = model.ReferringDoctorId;
            }

            if (visit.TreatmentRightId != model.TreatmentRightId)
            {
                changes.Add("- สิทธิการรักษา: เปลี่ยนแปลง");
                visit.TreatmentRightId = model.TreatmentRightId;
            }
            if (visit.Weight != model.Weight)
            {
                changes.Add($"- น้ำหนัก: {visit.Weight?.ToString("F1") ?? "-"} → {model.Weight?.ToString("F1") ?? "-"} กก.");
                visit.Weight = model.Weight;
            }
            if (visit.Height != model.Height)
            {
                changes.Add($"- ส่วนสูง: {visit.Height?.ToString("F1") ?? "-"} → {model.Height?.ToString("F1") ?? "-"} ซม.");
                visit.Height = model.Height;
            }
            var newWard = model.Ward?.Trim();
            if (visit.Ward != newWard)
            {
                changes.Add($"- Ward: '{visit.Ward ?? "-"}' → '{newWard ?? "-"}'");
                visit.Ward = newWard;
            }
            visit.Remark = model.Remark?.Trim();

            if (changes.Any())
                auditLines.Add("แก้ไขข้อมูลการลงทะเบียน:\n" + string.Join("\n", changes));
        }

        // --- Item changes ---
        {
            var changes = new List<string>();
            var oldMap = visit.PatientVisitItems.ToDictionary(x => x.ItemId);
            var newMap = validItems.Where(x => x.ItemId.HasValue).ToDictionary(x => x.ItemId!.Value);

            var removedIds = oldMap.Keys.Except(newMap.Keys).ToList();
            var addedIds = newMap.Keys.Except(oldMap.Keys).ToList();
            var allChangedIds = removedIds.Concat(addedIds).Distinct().ToList();

            Dictionary<int, string> changedNames = new();
            if (allChangedIds.Any())
                changedNames = (await _context.Items.Where(x => allChangedIds.Contains(x.ItemId))
                    .Select(x => new { x.ItemId, x.ItemName }).ToListAsync())
                    .ToDictionary(x => x.ItemId, x => x.ItemName);

            foreach (var rid in removedIds)
                changes.Add($"- ลบ: {changedNames.GetValueOrDefault(rid, $"Item #{rid}")}");
            foreach (var aid in addedIds)
                changes.Add($"- เพิ่ม: {changedNames.GetValueOrDefault(aid, $"Item #{aid}")}");

            foreach (var (itemId, ni) in newMap)
            {
                if (oldMap.TryGetValue(itemId, out var oi) && oi.Quantity != ni.Quantity)
                    changes.Add($"- เปลี่ยนจำนวน: {oi.Item?.ItemName ?? $"Item #{itemId}"} {oi.Quantity} → {ni.Quantity}");
            }

            if (changes.Any())
                auditLines.Add("แก้ไขรายการตรวจ:\n" + string.Join("\n", changes));

            // Apply item changes
            _context.PatientVisitItems.RemoveRange(visit.PatientVisitItems.ToList());
            foreach (var item in validItems)
            {
                _context.PatientVisitItems.Add(new PatientVisitItem
                {
                    PatientVisitId = visit.PatientVisitId,
                    ItemId = item.ItemId!.Value,
                    Quantity = item.Quantity
                });
            }
        }

        // --- Write audit log ---
        if (auditLines.Any())
            AddVisitAuditLog(visit.PatientVisitId, string.Join("\n\n", auditLines));

        // --- Sync to Invoice ---
        var invoice = visit.Invoice;
        if (invoice != null && invoice.Status == "Draft" && model.SyncInvoice)
        {
            if (patient != null)
            {
                invoice.PatientFullName = string.IsNullOrWhiteSpace(patient.NamePrefix)
                    ? $"{patient.FirstName} {patient.LastName}"
                    : $"{patient.NamePrefix} {patient.FirstName} {patient.LastName}";
                invoice.PatientHn = patient.HN;
                invoice.PatientGender = patient.Gender;
                invoice.PatientBirthDate = patient.DateOfBirth;
            }
            if (visit.CustomerId.HasValue)
                invoice.CustomerId = visit.CustomerId.Value;
            invoice.ReferringDoctorId = visit.ReferringDoctorId;
            invoice.TreatmentRightId = visit.TreatmentRightId;
            invoice.PatientWard = visit.Ward;
            invoice.Remark = visit.Remark;

            // Reload invoice items from updated visit items
            _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
            var itemIds = validItems.Select(x => x.ItemId!.Value).ToList();
            var itemsData = await _context.Items
                .Where(x => itemIds.Contains(x.ItemId))
                .ToDictionaryAsync(x => x.ItemId);

            var lineNum = 1;
            foreach (var vi in validItems)
            {
                if (!itemsData.TryGetValue(vi.ItemId!.Value, out var iData)) continue;
                var lineTotal = iData.UnitPrice * vi.Quantity;
                invoice.InvoiceDetails.Add(new InvoiceDetail
                {
                    LineNumber = lineNum++,
                    ItemId = iData.ItemId,
                    Qty = vi.Quantity,
                    QuotedQty = vi.Quantity,
                    UnitPrice = iData.UnitPrice,
                    DiscountAmount = 0,
                    LineTotal = lineTotal
                });
            }
            invoice.Subtotal = invoice.InvoiceDetails.Sum(x => x.LineTotal);
            invoice.VatAmount = 0;
            invoice.TotalAmount = invoice.Subtotal;
            invoice.BalanceAmount = invoice.TotalAmount;

            AddVisitAuditLog(visit.PatientVisitId, $"โหลดข้อมูลใหม่ไปที่ใบแจ้งหนี้ {invoice.InvoiceNo}");
        }

        await _context.SaveChangesAsync();

        TempData["VisitSuccess"] = "บันทึกการแก้ไขเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }
}
