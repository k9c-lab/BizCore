using System.Security.Claims;
using BizCore.Data;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Services;

public class UserPermissionService : IUserPermissionService
{
    private readonly AccountingDbContext _context;
    private string? _cachedRoleName;
    private HashSet<string>? _cachedPermissionCodes;
    private bool _hasAnyPermissionRecords;
    private bool _hasAnyMenuPermissionRecords;

    public UserPermissionService(AccountingDbContext context)
    {
        _context = context;
    }

    public bool HasMenuAccess(ClaimsPrincipal? user, string permissionCode)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole("Admin"))
        {
            return true;
        }

        var permissions = GetPermissionCodes(user);
        if (_hasAnyMenuPermissionRecords)
        {
            return permissions.Contains(permissionCode);
        }

        return LegacyRoleHasMenuAccess(user, permissionCode);
    }

    public bool HasPermission(ClaimsPrincipal? user, string permissionCode)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole("Admin") || user.IsInRole("Executive"))
        {
            return true;
        }

        var permissions = GetPermissionCodes(user);
        if (_hasAnyPermissionRecords)
        {
            return permissions.Contains(permissionCode);
        }

        return LegacyRoleHasPermission(user, permissionCode);
    }

    private HashSet<string> GetPermissionCodes(ClaimsPrincipal user)
    {
        var roleName = user.FindFirstValue(ClaimTypes.Role)?.Trim();
        if (_cachedPermissionCodes is not null && string.Equals(_cachedRoleName, roleName, StringComparison.OrdinalIgnoreCase))
        {
            return _cachedPermissionCodes;
        }

        _cachedRoleName = roleName;

        if (string.IsNullOrWhiteSpace(roleName))
        {
            _cachedPermissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _hasAnyPermissionRecords = false;
            _hasAnyMenuPermissionRecords = false;
            return _cachedPermissionCodes;
        }

        try
        {
            var permissionCodes = _context.RolePermissions
                .AsNoTracking()
                .Include(x => x.Permission)
                .Where(x => x.RoleName == roleName && x.Permission != null)
                .Select(x => x.Permission!.Code)
                .ToList();

            _cachedPermissionCodes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            _hasAnyPermissionRecords = _cachedPermissionCodes.Count > 0;
            _hasAnyMenuPermissionRecords = _cachedPermissionCodes.Any(x => x.EndsWith(".Menu", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is Microsoft.Data.SqlClient.SqlException || ex is InvalidOperationException)
        {
            _cachedPermissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _hasAnyPermissionRecords = false;
            _hasAnyMenuPermissionRecords = false;
        }

        return _cachedPermissionCodes;
    }

    private static bool LegacyRoleHasMenuAccess(ClaimsPrincipal user, string permissionCode)
    {
        if (user.IsInRole("Executive"))
        {
            return true;
        }

        return permissionCode switch
        {
            "Dashboard.Menu" => user.IsInRole("Viewer"),
            "Reports.Menu" or "FinancialOverview.Menu" or "InventoryOverview.Menu" =>
                user.IsInRole("CentralAdmin") || user.IsInRole("BranchAdmin") || user.IsInRole("Sales") || user.IsInRole("Warehouse"),
            "Sales.Menu" or "Sales.Quotations.Menu" or "Sales.Invoices.Menu" or "Sales.Payments.Menu" or "Sales.Receipts.Menu" =>
                user.IsInRole("Sales"),
            "Purchasing.Menu" or "Purchasing.PR.Menu" or "Purchasing.PO.Menu" or "Purchasing.Receiving.Menu" =>
                user.IsInRole("CentralAdmin") || user.IsInRole("BranchAdmin") || user.IsInRole("Warehouse"),
            "Inventory.Menu" or "Inventory.StockInquiry.Menu" or "Inventory.SerialInquiry.Menu" or "Inventory.StockLedger.Menu" or "Inventory.StockAudit.Menu" or "Inventory.StockTransfers.Menu" or "Inventory.StockIssues.Menu" =>
                user.IsInRole("BranchAdmin") || user.IsInRole("Warehouse"),
            "Warranty.Menu" or "Warranty.SupplierClaims.Menu" =>
                user.IsInRole("Warehouse"),
            "Warranty.CustomerClaims.Menu" =>
                user.IsInRole("BranchAdmin") || user.IsInRole("Warehouse"),
            _ => false
        };
    }

    private static bool LegacyRoleHasPermission(ClaimsPrincipal user, string permissionCode)
    {
        return permissionCode switch
        {
            "PR.View" or "PR.Create" or "PR.Edit" or "PR.Submit" or "PR.Cancel" =>
                user.IsInRole("BranchAdmin") || user.IsInRole("Warehouse"),
            "PR.Approve" or "PR.Reject" or "PO.Create" or "PO.Edit" or "PO.Submit" or "PO.Cancel" =>
                user.IsInRole("CentralAdmin"),
            "PO.View" or "PO.Receive" =>
                user.IsInRole("CentralAdmin") || user.IsInRole("BranchAdmin") || user.IsInRole("Warehouse"),
            "Receiving.View" =>
                user.IsInRole("CentralAdmin") || user.IsInRole("Executive") || user.IsInRole("BranchAdmin") || user.IsInRole("Warehouse"),
            "Receiving.Create" or "Receiving.Edit" or "Receiving.Post" or "Receiving.Cancel" =>
                user.IsInRole("BranchAdmin") || user.IsInRole("Warehouse"),
            "SupplierPayment.View" or "SupplierPayment.Create" or "SupplierPayment.Cancel" =>
                user.IsInRole("CentralAdmin"),
            "Reports.View" =>
                user.IsInRole("CentralAdmin") || user.IsInRole("BranchAdmin") || user.IsInRole("Sales") || user.IsInRole("Warehouse"),
            _ => false
        };
    }
}
