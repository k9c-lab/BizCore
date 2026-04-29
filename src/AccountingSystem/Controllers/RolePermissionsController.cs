using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class RolePermissionsController : CrudControllerBase
{
    private static readonly string[] Roles = { "Admin", "CentralAdmin", "BranchAdmin", "Sales", "Warehouse", "Executive", "Viewer" };
    private readonly AccountingDbContext _context;

    public RolePermissionsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? role)
    {
        var selectedRole = ResolveRole(role);
        var model = await BuildPageModelAsync(selectedRole);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string selectedRole, int[] permissionIds)
    {
        var role = ResolveRole(selectedRole);
        var selectedPermissionIds = permissionIds.Distinct().ToHashSet();
        var validPermissionIds = await _context.Permissions
            .AsNoTracking()
            .Where(x => selectedPermissionIds.Contains(x.PermissionId))
            .Select(x => x.PermissionId)
            .ToListAsync();

        var validPermissionIdSet = validPermissionIds.ToHashSet();
        var existing = await _context.RolePermissions
            .Where(x => x.RoleName == role)
            .ToListAsync();

        var toRemove = existing
            .Where(x => !validPermissionIdSet.Contains(x.PermissionId))
            .ToList();
        if (toRemove.Count > 0)
        {
            _context.RolePermissions.RemoveRange(toRemove);
        }

        var existingIds = existing.Select(x => x.PermissionId).ToHashSet();
        var toAdd = validPermissionIds
            .Where(id => !existingIds.Contains(id))
            .Select(id => new RolePermission
            {
                RoleName = role,
                PermissionId = id
            })
            .ToList();
        if (toAdd.Count > 0)
        {
            _context.RolePermissions.AddRange(toAdd);
        }

        await _context.SaveChangesAsync();

        TempData["RolePermissionNotice"] = $"Permissions for {role} were updated. Signed-in users should refresh the page if they are currently using this role.";
        return RedirectToAction(nameof(Index), new { role });
    }

    private async Task<RolePermissionPageViewModel> BuildPageModelAsync(string selectedRole)
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Code)
            .ToListAsync();

        var grantedIds = await _context.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleName == selectedRole)
            .Select(x => x.PermissionId)
            .ToListAsync();
        var grantedIdSet = grantedIds.ToHashSet();

        return new RolePermissionPageViewModel
        {
            SelectedRole = selectedRole,
            RoleOptions = BuildRoleOptions(selectedRole),
            Modules = permissions
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Module) ? "General" : x.Module)
                .Select(group => new RolePermissionModuleViewModel
                {
                    Module = group.Key,
                    Permissions = group
                        .Select(permission => new RolePermissionItemViewModel
                        {
                            PermissionId = permission.PermissionId,
                            Code = permission.Code,
                            Name = permission.Name,
                            IsGranted = grantedIdSet.Contains(permission.PermissionId)
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static string ResolveRole(string? role)
    {
        return Roles.Contains(role) ? role! : "BranchAdmin";
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
}
