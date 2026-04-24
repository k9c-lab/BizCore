using BizCore.Data;
using System.Security.Claims;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class WelcomeController : Controller
{
    private readonly AccountingDbContext _context;

    public WelcomeController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var branchName = User.FindFirst("CanAccessAllBranches")?.Value == "true" && !User.IsInRole("BranchAdmin")
            ? "All Branches"
            : User.FindFirst("BranchName")?.Value ?? string.Empty;

        var today = DateTime.Today;
        var announcements = await _context.Announcements
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => !x.PublishFromDate.HasValue || x.PublishFromDate.Value <= today)
            .Where(x => !x.PublishToDate.HasValue || x.PublishToDate.Value >= today)
            .OrderByDescending(x => x.PublishFromDate ?? x.CreatedDate.Date)
            .ThenByDescending(x => x.AnnouncementId)
            .Take(5)
            .Select(x => new WelcomeAnnouncementViewModel
            {
                Title = x.Title,
                Message = x.Message,
                StatusLabel = "Active",
                DateRangeLabel = (x.PublishFromDate.HasValue ? x.PublishFromDate.Value.ToString("dd MMM yyyy") : "Immediate")
                    + " - "
                    + (x.PublishToDate.HasValue ? x.PublishToDate.Value.ToString("dd MMM yyyy") : "Until changed")
            })
            .ToListAsync();

        var model = new WelcomeViewModel
        {
            DisplayName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "User",
            RoleName = User.FindFirstValue(ClaimTypes.Role) ?? "-",
            BranchName = branchName,
            CanUseDashboard = User.IsInRole("Admin") || User.HasClaim("Permission", "Dashboard.Menu"),
            CanUseFinancialOverview = User.IsInRole("Admin") || User.HasClaim("Permission", "FinancialOverview.Menu"),
            CanUseInventoryOverview = User.IsInRole("Admin") || User.HasClaim("Permission", "InventoryOverview.Menu"),
            CanUseAnnouncements = User.IsInRole("Admin") || User.HasClaim("Permission", "Announcements.Menu"),
            Announcements = announcements
        };

        return View(model);
    }
}
