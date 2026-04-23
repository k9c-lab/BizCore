using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class UsersController : CrudControllerBase
{
    private static readonly string[] Roles = { "Admin", "CentralAdmin", "BranchAdmin", "Sales", "Warehouse", "Executive", "Viewer" };
    private readonly AccountingDbContext _context;
    private readonly PasswordHashService _passwordHashService;

    public UsersController(AccountingDbContext context, PasswordHashService passwordHashService)
    {
        _context = context;
        _passwordHashService = passwordHashService;
    }

    public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1, int pageSize = 20)
    {
        ViewData["Search"] = search;
        ViewData["Role"] = role;
        ViewData["Status"] = status;

        IQueryable<User> query = _context.Users
            .AsNoTracking()
            .Include(x => x.Branch);
        var keyword = search?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.Username.Contains(keyword) ||
                x.DisplayName.Contains(keyword) ||
                (x.Email != null && x.Email.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(x => x.Role == role);
        }

        query = status switch
        {
            "Active" => query.Where(x => x.IsActive),
            "Inactive" => query.Where(x => !x.IsActive),
            _ => query
        };

        var users = await PaginatedList<User>.CreateAsync(query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Username), page, pageSize);

        return View(users);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.UserId == id.Value);

        return user is null ? NotFound() : View(user);
    }

    public async Task<IActionResult> Create()
    {
        var model = new UserFormViewModel
        {
            RoleOptions = BuildRoleOptions("Viewer"),
            BranchOptions = await BuildBranchOptionsAsync(null)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        model.UserId = null;

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required.");
        }

        await ValidateUserFormAsync(model);
        if (!ModelState.IsValid)
        {
            model.RoleOptions = BuildRoleOptions(model.Role);
            model.BranchOptions = await BuildBranchOptionsAsync(model.BranchId);
            return View(model);
        }

        var user = new User
        {
            Username = model.Username.Trim(),
            DisplayName = model.DisplayName.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            Role = model.Role,
            BranchId = model.BranchId,
            CanAccessAllBranches = model.CanAccessAllBranches,
            IsActive = model.IsActive,
            PasswordHash = _passwordHashService.HashPassword(model.Password!),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
            TempData["UserNotice"] = "User was created successfully.";
            return RedirectToAction(nameof(Details), new { id = user.UserId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            ModelState.AddModelError(string.Empty, "Username or email is already in use.");
        }

        model.RoleOptions = BuildRoleOptions(model.Role);
        model.BranchOptions = await BuildBranchOptionsAsync(model.BranchId);
        return View(model);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id.Value);
        if (user is null)
        {
            return NotFound();
        }

        var model = new UserFormViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role,
            BranchId = user.BranchId,
            CanAccessAllBranches = user.CanAccessAllBranches,
            IsActive = user.IsActive,
            RoleOptions = BuildRoleOptions(user.Role),
            BranchOptions = await BuildBranchOptionsAsync(user.BranchId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserFormViewModel model)
    {
        if (id != model.UserId)
        {
            return NotFound();
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id);
        if (user is null)
        {
            return NotFound();
        }

        await ValidateUserFormAsync(model);

        if (CurrentUserId() == user.UserId && (!model.IsActive || model.Role != "Admin"))
        {
            ModelState.AddModelError(string.Empty, "You cannot remove admin access or deactivate your own account.");
        }

        if (!ModelState.IsValid)
        {
            model.RoleOptions = BuildRoleOptions(model.Role);
            model.BranchOptions = await BuildBranchOptionsAsync(model.BranchId);
            return View(model);
        }

        user.Username = model.Username.Trim();
        user.DisplayName = model.DisplayName.Trim();
        user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        user.Role = model.Role;
        user.BranchId = model.BranchId;
        user.CanAccessAllBranches = model.CanAccessAllBranches;
        user.IsActive = model.IsActive;

        try
        {
            await _context.SaveChangesAsync();
            TempData["UserNotice"] = "User was updated successfully.";
            return RedirectToAction(nameof(Details), new { id = user.UserId });
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraintViolation(ex))
        {
            ModelState.AddModelError(string.Empty, "Username or email is already in use.");
        }

        model.RoleOptions = BuildRoleOptions(model.Role);
        model.BranchOptions = await BuildBranchOptionsAsync(model.BranchId);
        return View(model);
    }

    public async Task<IActionResult> ResetPassword(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id.Value);
        if (user is null)
        {
            return NotFound();
        }

        return View(new UserResetPasswordViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, UserResetPasswordViewModel model)
    {
        if (id != model.UserId)
        {
            return NotFound();
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.Username = user.Username;
            model.DisplayName = user.DisplayName;
            return View(model);
        }

        user.PasswordHash = _passwordHashService.HashPassword(model.NewPassword);
        await _context.SaveChangesAsync();

        TempData["UserNotice"] = "Password was reset successfully.";
        return RedirectToAction(nameof(Details), new { id = user.UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id);
        if (user is null)
        {
            return NotFound();
        }

        if (CurrentUserId() == user.UserId)
        {
            TempData["UserNotice"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Details), new { id = user.UserId });
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        TempData["UserNotice"] = user.IsActive ? "User was activated." : "User was deactivated.";
        return RedirectToAction(nameof(Details), new { id = user.UserId });
    }

    private async Task ValidateUserFormAsync(UserFormViewModel model)
    {
        model.Username = model.Username.Trim();
        model.DisplayName = model.DisplayName.Trim();
        model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();

        if (!Roles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Please select a valid role.");
        }

        if (model.Role == "BranchAdmin")
        {
            model.CanAccessAllBranches = false;
        }

        if (!model.BranchId.HasValue && !model.CanAccessAllBranches)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Please select a branch or allow access to all branches.");
        }

        if (model.Role == "BranchAdmin" && !model.BranchId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Branch Admin must be assigned to a branch.");
        }

        if (model.BranchId.HasValue)
        {
            var branchExists = await _context.Branches.AnyAsync(x => x.BranchId == model.BranchId.Value && x.IsActive);
            if (!branchExists)
            {
                ModelState.AddModelError(nameof(model.BranchId), "Please select an active branch.");
            }
        }

        var duplicateUsername = await _context.Users.AnyAsync(x =>
            x.Username == model.Username &&
            (!model.UserId.HasValue || x.UserId != model.UserId.Value));
        if (duplicateUsername)
        {
            ModelState.AddModelError(nameof(model.Username), "Username is already in use.");
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var duplicateEmail = await _context.Users.AnyAsync(x =>
                x.Email == model.Email &&
                (!model.UserId.HasValue || x.UserId != model.UserId.Value));
            if (duplicateEmail)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
            }
        }
    }

    private static IEnumerable<SelectListItem> BuildRoleOptions(string selectedRole)
    {
        return Roles.Select(role => new SelectListItem
        {
            Value = role,
            Text = role,
            Selected = role == selectedRole
        });
    }

    private async Task<IEnumerable<SelectListItem>> BuildBranchOptionsAsync(int? selectedBranchId)
    {
        var branches = await _context.Branches
            .AsNoTracking()
            .Where(x => x.IsActive || x.BranchId == selectedBranchId)
            .OrderBy(x => x.BranchCode)
            .Select(x => new SelectListItem
            {
                Value = x.BranchId.ToString(),
                Text = x.BranchCode + " - " + x.BranchName,
                Selected = selectedBranchId.HasValue && x.BranchId == selectedBranchId.Value
            })
            .ToListAsync();

        branches.Insert(0, new SelectListItem("Select branch", string.Empty, !selectedBranchId.HasValue));
        return branches;
    }
}
