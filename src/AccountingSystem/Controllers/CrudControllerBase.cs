using System.Text.RegularExpressions;
using System.Security.Claims;
using BizCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

public abstract class CrudControllerBase : Controller
{
    private static readonly Regex TrailingDigitsRegex = new(@"\d+$", RegexOptions.Compiled);
    private static readonly IReadOnlyDictionary<string, string> ControllerMenuPermissions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Reports"] = "Reports.Menu",
        ["Quotations"] = "Sales.Quotations.Menu",
        ["Invoices"] = "Sales.Invoices.Menu",
        ["Payments"] = "Sales.Payments.Menu",
        ["Receipts"] = "Sales.Receipts.Menu",
        ["PurchaseRequests"] = "Purchasing.PR.Menu",
        ["PurchaseOrders"] = "Purchasing.PO.Menu",
        ["Receivings"] = "Purchasing.Receiving.Menu",
        ["StockInquiry"] = "Inventory.StockInquiry.Menu",
        ["SerialInquiry"] = "Inventory.SerialInquiry.Menu",
        ["StockLedger"] = "Inventory.StockLedger.Menu",
        ["StockAudit"] = "Inventory.StockAudit.Menu",
        ["StockTransfers"] = "Inventory.StockTransfers.Menu",
        ["StockIssues"] = "Inventory.StockIssues.Menu",
        ["CustomerClaims"] = "Warranty.CustomerClaims.Menu",
        ["SupplierClaims"] = "Warranty.SupplierClaims.Menu",
        ["Branches"] = "MasterData.Branches.Menu",
        ["Customers"] = "MasterData.Customers.Menu",
        ["Suppliers"] = "MasterData.Suppliers.Menu",
        ["Salespersons"] = "MasterData.Salespersons.Menu",
        ["Items"] = "MasterData.Items.Menu",
        ["Users"] = "MasterData.Users.Menu",
        ["RolePermissions"] = "MasterData.RolePermissions.Menu"
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (User.IsInRole("Admin"))
        {
            return;
        }

        var controllerName = context.RouteData.Values["controller"]?.ToString();
        if (string.IsNullOrWhiteSpace(controllerName))
        {
            return;
        }

        if (!ControllerMenuPermissions.TryGetValue(controllerName, out var menuPermissionCode))
        {
            return;
        }

        if (!CurrentUserHasMenuAccess(menuPermissionCode))
        {
            context.Result = new ForbidResult();
        }
    }

    protected static bool IsDuplicateConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
            || exception.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    protected static string Format4DigitCode(int sequence)
    {
        return Math.Clamp(sequence, 1, 9999).ToString("D4");
    }

    protected static string FormatPrefixedCode(string prefix, int sequence)
    {
        return $"{prefix}-{Format4DigitCode(sequence)}";
    }

    protected static string FormatPeriodPrefixedCode(string prefix, DateTime date, int sequence)
    {
        return $"{prefix}-{date:yyyyMM}-{Format4DigitCode(sequence)}";
    }

    protected static int ExtractSequence(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return 0;
        }

        var match = TrailingDigitsRegex.Match(code.Trim());
        return match.Success && int.TryParse(match.Value, out var sequence)
            ? sequence
            : 0;
    }

    protected int? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    protected int? CurrentBranchId()
    {
        var value = User.FindFirstValue("BranchId");
        return int.TryParse(value, out var branchId) ? branchId : null;
    }

    protected bool CurrentUserCanAccessAllBranches()
    {
        return !User.IsInRole("BranchAdmin")
            && (string.Equals(User.FindFirstValue("CanAccessAllBranches"), "true", StringComparison.OrdinalIgnoreCase)
                || User.IsInRole("Admin")
                || User.IsInRole("Executive"));
    }

    protected bool CurrentUserHasMenuAccess(string permissionCode)
    {
        return User.IsInRole("Admin") || User.HasClaim("Permission", permissionCode);
    }

    protected void PopulatePrintCompanyViewData(CompanyProfileSettings companyProfile)
    {
        ViewData["PrintCompanyName"] = string.IsNullOrWhiteSpace(companyProfile.Name) ? "-" : companyProfile.Name.Trim();
        ViewData["PrintCompanyAddress"] = string.IsNullOrWhiteSpace(companyProfile.Address) ? "-" : companyProfile.Address.Trim();
        ViewData["PrintCompanyTaxId"] = string.IsNullOrWhiteSpace(companyProfile.TaxId) ? "-" : companyProfile.TaxId.Trim();
        ViewData["PrintCompanyPhone"] = string.IsNullOrWhiteSpace(companyProfile.Phone) ? "-" : companyProfile.Phone.Trim();
        ViewData["PrintCompanyEmail"] = companyProfile.Email?.Trim() ?? string.Empty;
    }

    protected bool CurrentUserHasPermission(string permissionCode)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Executive") || User.HasClaim("Permission", permissionCode))
        {
            return true;
        }

        if (User.HasClaim(x => x.Type == "Permission"))
        {
            return false;
        }

        return permissionCode switch
        {
            "PR.View" or "PR.Create" or "PR.Edit" or "PR.Submit" or "PR.Cancel" =>
                User.IsInRole("BranchAdmin") || User.IsInRole("Warehouse"),
            "PR.Approve" or "PR.Reject" or "PO.Create" or "PO.Edit" or "PO.Submit" or "PO.Cancel" =>
                User.IsInRole("CentralAdmin"),
            "PO.View" or "PO.Receive" =>
                User.IsInRole("CentralAdmin") || User.IsInRole("BranchAdmin") || User.IsInRole("Warehouse"),
            "Receiving.View" =>
                User.IsInRole("CentralAdmin") || User.IsInRole("Executive") || User.IsInRole("BranchAdmin") || User.IsInRole("Warehouse"),
            "Receiving.Create" or "Receiving.Edit" or "Receiving.Post" or "Receiving.Cancel" =>
                User.IsInRole("BranchAdmin") || User.IsInRole("Warehouse"),
            "Reports.View" =>
                User.IsInRole("CentralAdmin") || User.IsInRole("BranchAdmin") || User.IsInRole("Sales") || User.IsInRole("Warehouse"),
            _ => false
        };
    }
}
