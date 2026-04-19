using BizCore.Data;
using BizCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin")]
public class ItemsController : CrudControllerBase
{
    private const string CodePrefix = "ITM";
    private readonly AccountingDbContext _context;

    public ItemsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Items.OrderBy(x => x.ItemCode).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new Item
        {
            ItemCode = await GetNextItemCodeAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item)
    {
        ViewData["CodeReadOnly"] = true;
        item.ItemCode = await EnsureItemCodeAsync(item.ItemCode);
        ModelState.Remove(nameof(Item.ItemCode));

        if (!ModelState.IsValid)
        {
            return View(item);
        }

        _context.Items.Add(item);
        if (!await TrySaveAsync("Item code and part number must be unique."))
        {
            return View(item);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.Items.FindAsync(id.Value);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Item item)
    {
        if (id != item.ItemId)
        {
            return NotFound();
        }

        ViewData["CodeReadOnly"] = false;
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        _context.Update(item);
        if (!await TrySaveAsync("Item code and part number must be unique."))
        {
            return View(item);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.Items.FirstOrDefaultAsync(x => x.ItemId == id.Value);
        return item is null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var item = await _context.Items.FirstOrDefaultAsync(x => x.ItemId == id.Value);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item is not null)
        {
            _context.Items.Remove(item);
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

    private Task<string> GetNextItemCodeAsync()
    {
        return GetNextCodeAsync(_context.Items.Select(x => x.ItemCode));
    }

    private async Task<string> EnsureItemCodeAsync(string? existingCode)
    {
        return string.IsNullOrWhiteSpace(existingCode)
            ? await GetNextItemCodeAsync()
            : existingCode.Trim();
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery)
    {
        var codes = await codesQuery.ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
