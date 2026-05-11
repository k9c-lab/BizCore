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
    private static readonly string[] Roles = { "Admin", "CentralAdmin", "BranchAdmin", "Sales", "Accounting", "Warehouse", "Executive", "Viewer" };
    private static readonly Dictionary<string, (string Title, string Description, int SortOrder)> SectionMetadata = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Overview"] = ("Overview", "แดชบอร์ด รายงาน และภาพรวมระบบ", 10),
        ["Sales"] = ("Sales", "เมนูและสิทธิ์ของงานขาย เอกสารลูกหนี้ และรับชำระ", 20),
        ["Purchasing"] = ("Purchasing", "งานจัดซื้อ รับสินค้า และจ่ายผู้ขาย", 30),
        ["Inventory"] = ("Inventory", "สต็อก สอบถามคงเหลือ และเอกสารเคลื่อนไหวสินค้า", 40),
        ["Warranty"] = ("Warranty", "งานเคลมลูกค้าและเคลมผู้ขาย", 50),
        ["MasterData"] = ("Master Data", "เมนูตั้งค่าข้อมูลหลักและผู้ดูแลระบบ", 60),
        ["General"] = ("General", "สิทธิ์อื่นๆ ที่ยังไม่ได้จัดเข้าหมวดหลัก", 999)
    };
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

        var modules = permissions
            .Select(permission =>
            {
                var sectionKey = ResolveSectionKey(permission.Code);
                var groupTitle = ResolveGroupTitle(permission.Code);
                var kindLabel = permission.Code.EndsWith(".Menu", StringComparison.OrdinalIgnoreCase) ? "Menu" : "Action";
                var displayName = ResolveDisplayName(permission.Code, permission.Name);

                return new
                {
                    SectionKey = sectionKey,
                    GroupTitle = groupTitle,
                    Permission = new RolePermissionItemViewModel
                    {
                        PermissionId = permission.PermissionId,
                        Code = permission.Code,
                        Name = permission.Name,
                        DisplayName = displayName,
                        KindLabel = kindLabel,
                        IsGranted = grantedIdSet.Contains(permission.PermissionId)
                    }
                };
            })
            .GroupBy(x => x.SectionKey)
            .Select(sectionGroup =>
            {
                var metadata = SectionMetadata.TryGetValue(sectionGroup.Key, out var sectionMeta)
                    ? sectionMeta
                    : (Title: HumanizeToken(sectionGroup.Key), Description: string.Empty, SortOrder: 999);

                var groups = sectionGroup
                    .GroupBy(x => x.GroupTitle)
                    .Select(group => new RolePermissionGroupViewModel
                    {
                        Title = group.Key,
                        PermissionCount = group.Count(),
                        GrantedCount = group.Count(x => x.Permission.IsGranted),
                        Permissions = group
                            .Select(x => x.Permission)
                            .OrderByDescending(x => x.KindLabel == "Menu")
                            .ThenBy(x => x.DisplayName)
                            .ToList()
                    })
                    .OrderBy(group => GroupSortWeight(group.Title))
                    .ThenBy(group => group.Title)
                    .ToList();

                return new RolePermissionModuleViewModel
                {
                    Module = metadata.Title,
                    Description = metadata.Description,
                    PermissionCount = groups.Sum(x => x.PermissionCount),
                    GrantedCount = groups.Sum(x => x.GrantedCount),
                    Groups = groups
                };
            })
            .OrderBy(x => SectionMetadata.TryGetValue(NormalizeSectionKey(x.Module), out var metadata) ? metadata.SortOrder : 999)
            .ThenBy(x => x.Module)
            .ToList();

        return new RolePermissionPageViewModel
        {
            SelectedRole = selectedRole,
            RoleOptions = BuildRoleOptions(selectedRole),
            Modules = modules,
            PermissionCount = modules.Sum(x => x.PermissionCount),
            GrantedCount = modules.Sum(x => x.GrantedCount)
        };
    }

    private static string ResolveSectionKey(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "General";
        }

        if (code.StartsWith("Sales.", StringComparison.OrdinalIgnoreCase))
        {
            return "Sales";
        }

        if (code.StartsWith("Purchasing.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("PR.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("PO.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("Receiving.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("SupplierPayment.", StringComparison.OrdinalIgnoreCase))
        {
            return "Purchasing";
        }

        if (code.StartsWith("Inventory.", StringComparison.OrdinalIgnoreCase))
        {
            return "Inventory";
        }

        if (code.StartsWith("Warranty.", StringComparison.OrdinalIgnoreCase))
        {
            return "Warranty";
        }

        if (code.StartsWith("MasterData.", StringComparison.OrdinalIgnoreCase))
        {
            return "MasterData";
        }

        if (code.StartsWith("Dashboard.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("Reports.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("FinancialOverview.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("InventoryOverview.", StringComparison.OrdinalIgnoreCase) ||
            code.StartsWith("Announcements.", StringComparison.OrdinalIgnoreCase))
        {
            return "Overview";
        }

        return "General";
    }

    private static string ResolveGroupTitle(string code)
    {
        var parts = code.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "Other";
        }

        if (parts[^1].Equals("Menu", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length == 2)
            {
                return "Main Menu";
            }

            return HumanizeToken(parts[^2]);
        }

        return HumanizeToken(parts[0]);
    }

    private static string ResolveDisplayName(string code, string name)
    {
        var parts = code.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return string.IsNullOrWhiteSpace(name) ? code : name;
        }

        if (parts[^1].Equals("Menu", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length == 2)
            {
                return $"{HumanizeToken(parts[0])} Menu";
            }

            return $"{HumanizeToken(parts[^2])} Menu";
        }

        return HumanizeToken(parts[^1]);
    }

    private static string HumanizeToken(string token)
    {
        return token switch
        {
            "PR" => "Purchase Requests",
            "PO" => "Purchase Orders",
            "SupplierPayment" => "Supplier Payments",
            "RolePermissions" => "Role Permissions",
            "PriceLevels" => "Price Levels",
            "TreatmentRights" => "Treatment Rights",
            "ReferringDoctors" => "Doctors",
            "CashSales" => "Cash Sales",
            "BillingNotes" => "Billing Notes",
            "StockInquiry" => "Stock Inquiry",
            "SerialInquiry" => "Serial Inquiry",
            "StockLedger" => "Stock Ledger",
            "StockAudit" => "Stock Audit",
            "StockTransfers" => "Stock Transfers",
            "StockIssues" => "Stock Issues",
            "FinancialOverview" => "Financial Overview",
            "InventoryOverview" => "Inventory Overview",
            _ => System.Text.RegularExpressions.Regex.Replace(token, "(\\B[A-Z])", " $1")
        };
    }

    private static int GroupSortWeight(string title)
    {
        return string.Equals(title, "Main Menu", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    private static string NormalizeSectionKey(string moduleTitle)
    {
        return SectionMetadata.FirstOrDefault(x => x.Value.Title == moduleTitle).Key ?? moduleTitle;
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
