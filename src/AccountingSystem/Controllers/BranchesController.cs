using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin")]
public class BranchesController : CrudControllerBase
{
    private const string CodePrefix = "BR";
    private readonly AccountingDbContext _context;

    public BranchesController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var query = _context.Branches.AsNoTracking();
        var keyword = search?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.BranchCode.Contains(keyword) ||
                x.BranchName.Contains(keyword) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword)) ||
                (x.Email != null && x.Email.Contains(keyword)));
        }

        query = status switch
        {
            "Active" => query.Where(x => x.IsActive),
            "Inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        return View(await PaginatedList<Branch>.CreateAsync(query.OrderBy(x => x.BranchCode), page, pageSize));
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CodeReadOnly"] = true;
        return View(new Branch
        {
            BranchCode = await GetNextBranchCodeAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Branch branch)
    {
        ViewData["CodeReadOnly"] = true;
        branch.BranchCode = await EnsureBranchCodeAsync(branch.BranchCode);
        ModelState.Remove(nameof(Branch.BranchCode));

        if (!ModelState.IsValid)
        {
            return View(branch);
        }

        _context.Branches.Add(branch);
        if (!await TrySaveAsync("Branch code must be unique."))
        {
            return View(branch);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var branch = await _context.Branches.FindAsync(id.Value);
        return branch is null ? NotFound() : View(branch);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Branch branch)
    {
        if (id != branch.BranchId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(branch);
        }

        _context.Update(branch);
        if (!await TrySaveAsync("Branch code must be unique."))
        {
            return View(branch);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.BranchId == id.Value);
        return branch is null ? NotFound() : View(branch);
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

    private Task<string> GetNextBranchCodeAsync()
    {
        return GetNextCodeAsync(_context.Branches.Select(x => x.BranchCode));
    }

    private async Task<string> EnsureBranchCodeAsync(string? existingCode)
    {
        return string.IsNullOrWhiteSpace(existingCode)
            ? await GetNextBranchCodeAsync()
            : existingCode.Trim();
    }

    private static async Task<string> GetNextCodeAsync(IQueryable<string> codesQuery)
    {
        var codes = await codesQuery.ToListAsync();
        var nextSequence = codes.Select(ExtractSequence).DefaultIfEmpty(0).Max() + 1;
        return FormatPrefixedCode(CodePrefix, nextSequence);
    }
}
