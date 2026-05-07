using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class TreatmentRightsController : CrudControllerBase
{
    private const string CodePrefix = "TRT";
    private readonly AccountingDbContext _context;

    public TreatmentRightsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _context.TreatmentRights.AsNoTracking();
        var keyword = search?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.TreatmentRightCode.Contains(keyword) ||
                x.TreatmentRightName.Contains(keyword));
        }

        query = status switch
        {
            "Active" => query.Where(x => x.IsActive),
            "Inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        return View(await PaginatedList<TreatmentRight>.CreateAsync(query.OrderBy(x => x.TreatmentRightCode), page, pageSize));
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new TreatmentRight
        {
            TreatmentRightCode = await GetNextTreatmentRightCodeAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TreatmentRight treatmentRight)
    {
        ViewData["CodeReadOnly"] = true;
        treatmentRight.TreatmentRightCode = await EnsureTreatmentRightCodeAsync(treatmentRight.TreatmentRightCode);
        ModelState.Remove(nameof(TreatmentRight.TreatmentRightCode));

        if (!ModelState.IsValid)
        {
            return View(treatmentRight);
        }

        _context.TreatmentRights.Add(treatmentRight);
        if (!await TrySaveAsync("รหัสสิทธิการรักษาซ้ำหรือถูกใช้งานแล้ว"))
        {
            return View(treatmentRight);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var treatmentRight = await _context.TreatmentRights.FindAsync(id.Value);
        return treatmentRight is null ? NotFound() : View(treatmentRight);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TreatmentRight treatmentRight)
    {
        if (id != treatmentRight.TreatmentRightId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(treatmentRight);
        }

        _context.Update(treatmentRight);
        if (!await TrySaveAsync("รหัสสิทธิการรักษาซ้ำหรือถูกใช้งานแล้ว"))
        {
            return View(treatmentRight);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var treatmentRight = await _context.TreatmentRights.FirstOrDefaultAsync(x => x.TreatmentRightId == id.Value);
        return treatmentRight is null ? NotFound() : View(treatmentRight);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var treatmentRight = await _context.TreatmentRights.FirstOrDefaultAsync(x => x.TreatmentRightId == id.Value);
        return treatmentRight is null ? NotFound() : View(treatmentRight);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var treatmentRight = await _context.TreatmentRights.FindAsync(id);
        if (treatmentRight is not null)
        {
            _context.TreatmentRights.Remove(treatmentRight);
            if (!await TrySaveAsync("ไม่สามารถลบสิทธิการรักษานี้ได้ เพราะมีเอกสารอ้างอิงอยู่"))
            {
                return View("Delete", treatmentRight);
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

    private Task<string> GetNextTreatmentRightCodeAsync()
    {
        return GetNextCodeAsync(_context.TreatmentRights.Select(x => x.TreatmentRightCode));
    }

    private async Task<string> EnsureTreatmentRightCodeAsync(string? existingCode)
    {
        return string.IsNullOrWhiteSpace(existingCode)
            ? await GetNextTreatmentRightCodeAsync()
            : existingCode.Trim();
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery)
    {
        var codes = await codesQuery.ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
