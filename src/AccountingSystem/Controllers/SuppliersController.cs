using BizCore.Data;
using BizCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin")]
public class SuppliersController : CrudControllerBase
{
    private const string CodePrefix = "SUP";
    private readonly AccountingDbContext _context;

    public SuppliersController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Suppliers.OrderBy(x => x.SupplierCode).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new Supplier
        {
            SupplierCode = await GetNextSupplierCodeAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        ViewData["CodeReadOnly"] = true;
        supplier.SupplierCode = await EnsureSupplierCodeAsync(supplier.SupplierCode);
        ModelState.Remove(nameof(Supplier.SupplierCode));

        if (!ModelState.IsValid)
        {
            return View(supplier);
        }

        _context.Suppliers.Add(supplier);
        if (!await TrySaveAsync("Supplier code or email must be unique."))
        {
            return View(supplier);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers.FindAsync(id.Value);
        return supplier is null ? NotFound() : View(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Supplier supplier)
    {
        if (id != supplier.SupplierId)
        {
            return NotFound();
        }

        ViewData["CodeReadOnly"] = false;
        if (!ModelState.IsValid)
        {
            return View(supplier);
        }

        _context.Update(supplier);
        if (!await TrySaveAsync("Supplier code or email must be unique."))
        {
            return View(supplier);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.SupplierId == id.Value);
        return supplier is null ? NotFound() : View(supplier);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.SupplierId == id.Value);
        return supplier is null ? NotFound() : View(supplier);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is not null)
        {
            _context.Suppliers.Remove(supplier);
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

    private Task<string> GetNextSupplierCodeAsync()
    {
        return GetNextCodeAsync(_context.Suppliers.Select(x => x.SupplierCode));
    }

    private async Task<string> EnsureSupplierCodeAsync(string? existingCode)
    {
        return string.IsNullOrWhiteSpace(existingCode)
            ? await GetNextSupplierCodeAsync()
            : existingCode.Trim();
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery)
    {
        var codes = await codesQuery.ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
