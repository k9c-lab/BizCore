using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class PatientsController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public PatientsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _context.Patients.AsNoTracking();
        var keyword = search?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.HN.Contains(keyword) ||
                x.FirstName.Contains(keyword) ||
                x.LastName.Contains(keyword) ||
                (x.NationalId != null && x.NationalId.Contains(keyword)) ||
                (x.Phone != null && x.Phone.Contains(keyword)));
        }

        query = status switch
        {
            "Active" => query.Where(x => x.IsActive),
            "Inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        return View(await PaginatedList<Patient>.CreateAsync(
            query.OrderBy(x => x.HN), page, pageSize));
    }

    public IActionResult Create()
    {
        return View(new Patient());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Patient patient)
    {
        if (!ModelState.IsValid)
        {
            return View(patient);
        }

        _context.Patients.Add(patient);
        if (!await TrySaveAsync("HN นี้มีอยู่แล้ว"))
        {
            return View(patient);
        }

        return RedirectToAction(nameof(Details), new { id = patient.PatientId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var patient = await _context.Patients.FindAsync(id.Value);
        return patient is null ? NotFound() : View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Patient patient)
    {
        if (id != patient.PatientId) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(patient);
        }

        _context.Update(patient);
        if (!await TrySaveAsync("HN นี้มีอยู่แล้ว"))
        {
            return View(patient);
        }

        return RedirectToAction(nameof(Details), new { id = patient.PatientId });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();

        var patient = await _context.Patients
            .AsNoTracking()
            .Include(x => x.PatientVisits)
                .ThenInclude(x => x.TreatmentRight)
            .Include(x => x.PatientVisits)
                .ThenInclude(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.PatientId == id.Value);

        return patient is null ? NotFound() : View(patient);
    }

    private async Task<bool> TrySaveAsync(string errorMessage)
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, errorMessage);
            return false;
        }
    }
}
