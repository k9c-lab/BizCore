using BizCore.Data;
using BizCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin")]
public class CustomersController : CrudControllerBase
{
    private const string CodePrefix = "CUS";
    private readonly AccountingDbContext _context;

    public CustomersController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Customers.OrderBy(x => x.CustomerCode).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new Customer
        {
            CustomerCode = await GetNextCustomerCodeAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        ViewData["CodeReadOnly"] = true;
        customer.CustomerCode = await EnsureCustomerCodeAsync(customer.CustomerCode);
        ModelState.Remove(nameof(Customer.CustomerCode));

        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        _context.Customers.Add(customer);
        if (!await TrySaveAsync("Customer code or email must be unique."))
        {
            return View(customer);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FindAsync(id.Value);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId)
        {
            return NotFound();
        }

        ViewData["CodeReadOnly"] = false;
        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        _context.Update(customer);
        if (!await TrySaveAsync("Customer code or email must be unique."))
        {
            return View(customer);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.CustomerId == id.Value);
        return customer is null ? NotFound() : View(customer);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(x => x.CustomerId == id.Value);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer is not null)
        {
            _context.Customers.Remove(customer);
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

    private Task<string> GetNextCustomerCodeAsync()
    {
        return GetNextCodeAsync(_context.Customers.Select(x => x.CustomerCode));
    }

    private async Task<string> EnsureCustomerCodeAsync(string? existingCode)
    {
        return string.IsNullOrWhiteSpace(existingCode)
            ? await GetNextCustomerCodeAsync()
            : existingCode.Trim();
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery)
    {
        var codes = await codesQuery.ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
