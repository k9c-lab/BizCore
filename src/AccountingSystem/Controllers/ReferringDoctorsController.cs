using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class ReferringDoctorsController : CrudControllerBase
{
    private const string CodePrefix = "DOC";
    private readonly AccountingDbContext _context;

    public ReferringDoctorsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _context.ReferringDoctors.AsNoTracking();
        var keyword = search?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.DoctorCode.Contains(keyword) ||
                x.DoctorName.Contains(keyword));
        }

        query = status switch
        {
            "Active" => query.Where(x => x.IsActive),
            "Inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        return View(await PaginatedList<ReferringDoctor>.CreateAsync(query.OrderBy(x => x.DoctorCode), page, pageSize));
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new ReferringDoctor
        {
            DoctorCode = await GetNextDoctorCodeAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReferringDoctor doctor)
    {
        ViewData["CodeReadOnly"] = true;
        doctor.DoctorCode = await EnsureDoctorCodeAsync(doctor.DoctorCode);
        ModelState.Remove(nameof(ReferringDoctor.DoctorCode));

        if (!ModelState.IsValid)
        {
            return View(doctor);
        }

        _context.ReferringDoctors.Add(doctor);
        if (!await TrySaveAsync("รหัสแพทย์ส่งซ้ำหรือถูกใช้งานแล้ว"))
        {
            return View(doctor);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var doctor = await _context.ReferringDoctors.FindAsync(id.Value);
        return doctor is null ? NotFound() : View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReferringDoctor doctor)
    {
        if (id != doctor.ReferringDoctorId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(doctor);
        }

        _context.Update(doctor);
        if (!await TrySaveAsync("รหัสแพทย์ส่งซ้ำหรือถูกใช้งานแล้ว"))
        {
            return View(doctor);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var doctor = await _context.ReferringDoctors.FirstOrDefaultAsync(x => x.ReferringDoctorId == id.Value);
        return doctor is null ? NotFound() : View(doctor);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var doctor = await _context.ReferringDoctors.FirstOrDefaultAsync(x => x.ReferringDoctorId == id.Value);
        return doctor is null ? NotFound() : View(doctor);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var doctor = await _context.ReferringDoctors.FindAsync(id);
        if (doctor is not null)
        {
            _context.ReferringDoctors.Remove(doctor);
            if (!await TrySaveAsync("ไม่สามารถลบแพทย์ส่งนี้ได้ เพราะมีเอกสารอ้างอิงอยู่"))
            {
                return View("Delete", doctor);
            }
        }

        return RedirectToAction(nameof(Index));
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

    private Task<string> GetNextDoctorCodeAsync()
    {
        return GetNextCodeAsync(_context.ReferringDoctors.Select(x => x.DoctorCode));
    }

    private async Task<string> EnsureDoctorCodeAsync(string? existingCode)
    {
        return string.IsNullOrWhiteSpace(existingCode)
            ? await GetNextDoctorCodeAsync()
            : existingCode.Trim();
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery)
    {
        var codes = await codesQuery.ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
