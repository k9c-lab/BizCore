using BizCore.Data;
using BizCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin")]
public class PriceLevelsController : CrudControllerBase
{
    private const string CodePrefix = "PRL";
    private readonly AccountingDbContext _context;

    public PriceLevelsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var levels = await _context.PriceLevels
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PriceLevelCode)
            .ToListAsync();
        return View(levels);
    }

    public IActionResult Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new PriceLevel
        {
            PriceLevelCode = GetNextPriceLevelCode(),
            IsActive = true,
            SortOrder = 10
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PriceLevel model)
    {
        ViewData["CodeReadOnly"] = true;
        model.PriceLevelCode = GetNextPriceLevelCode();
        ModelState.Remove(nameof(PriceLevel.PriceLevelCode));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.PriceLevels.Add(model);
        if (!await TrySaveAsync("Price level code must be unique."))
        {
            return View(model);
        }

        TempData["PriceLevelNotice"] = "Price level created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var level = await _context.PriceLevels.FindAsync(id.Value);
        ViewData["CodeReadOnly"] = false;
        return level is null ? NotFound() : View(level);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PriceLevel model)
    {
        if (id != model.PriceLevelId)
        {
            return NotFound();
        }

        ViewData["CodeReadOnly"] = false;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.Update(model);
        if (!await TrySaveAsync("Price level code must be unique."))
        {
            return View(model);
        }

        TempData["PriceLevelNotice"] = "Price level updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var level = await _context.PriceLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PriceLevelId == id.Value);
        return level is null ? NotFound() : View(level);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var level = await _context.PriceLevels
            .Include(x => x.ItemPrices)
            .FirstOrDefaultAsync(x => x.PriceLevelId == id);

        if (level is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (level.ItemPrices.Count > 0)
        {
            TempData["PriceLevelNotice"] = "Cannot delete this price level because it is already used by item selling prices.";
            return RedirectToAction(nameof(Index));
        }

        _context.PriceLevels.Remove(level);
        await _context.SaveChangesAsync();
        TempData["PriceLevelNotice"] = "Price level deleted successfully.";
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

    private string GetNextPriceLevelCode()
    {
        var codePrefix = $"{CodePrefix}-";
        var codes = _context.PriceLevels
            .AsNoTracking()
            .Where(x => x.PriceLevelCode.StartsWith(codePrefix))
            .Select(x => x.PriceLevelCode)
            .ToList();

        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
