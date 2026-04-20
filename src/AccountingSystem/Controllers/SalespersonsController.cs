using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin")]
public class SalespersonsController : CrudControllerBase
{
    private const string CodePrefix = "SAL";
    private readonly AccountingDbContext _context;

    public SalespersonsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _context.Salespersons.AsNoTracking();
        var keyword = search?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.SalespersonCode.Contains(keyword) ||
                x.SalespersonName.Contains(keyword) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword)) ||
                (x.Email != null && x.Email.Contains(keyword)));
        }

        query = status switch
        {
            "Active" => query.Where(x => x.IsActive),
            "Inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        return View(await PaginatedList<Salesperson>.CreateAsync(query.OrderBy(x => x.SalespersonCode), page, pageSize));
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new Salesperson
        {
            SalespersonCode = await GetNextSalespersonCodeAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Salesperson salesperson)
    {
        ViewData["CodeReadOnly"] = true;
        salesperson.SalespersonCode = await EnsureSalespersonCodeAsync(salesperson.SalespersonCode);
        ModelState.Remove(nameof(Salesperson.SalespersonCode));

        if (!ModelState.IsValid)
        {
            return View(salesperson);
        }

        _context.Salespersons.Add(salesperson);
        if (!await TrySaveAsync("Salesperson code or email must be unique."))
        {
            return View(salesperson);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var salesperson = await _context.Salespersons.FindAsync(id.Value);
        return salesperson is null ? NotFound() : View(salesperson);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Salesperson salesperson)
    {
        if (id != salesperson.SalespersonId)
        {
            return NotFound();
        }

        ViewData["CodeReadOnly"] = false;
        if (!ModelState.IsValid)
        {
            return View(salesperson);
        }

        _context.Update(salesperson);
        if (!await TrySaveAsync("Salesperson code or email must be unique."))
        {
            return View(salesperson);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var salesperson = await _context.Salespersons.FirstOrDefaultAsync(x => x.SalespersonId == id.Value);
        return salesperson is null ? NotFound() : View(salesperson);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var salesperson = await _context.Salespersons.FirstOrDefaultAsync(x => x.SalespersonId == id.Value);
        return salesperson is null ? NotFound() : View(salesperson);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var salesperson = await _context.Salespersons.FindAsync(id);
        if (salesperson is not null)
        {
            _context.Salespersons.Remove(salesperson);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
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

    private Task<string> GetNextSalespersonCodeAsync()
    {
        return GetNextCodeAsync(_context.Salespersons.Select(x => x.SalespersonCode));
    }

    private async Task<string> EnsureSalespersonCodeAsync(string? existingCode)
    {
        return string.IsNullOrWhiteSpace(existingCode)
            ? await GetNextSalespersonCodeAsync()
            : existingCode.Trim();
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery)
    {
        var codes = await codesQuery.ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
