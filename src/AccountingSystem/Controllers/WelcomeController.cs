using BizCore.Data;
using System.Security.Claims;
using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class WelcomeController : Controller
{
    private static readonly string[] AnnouncementAccentClasses =
    {
        "note-amber",
        "note-mint",
        "note-sky",
        "note-peach",
        "note-lilac"
    };

    private readonly AccountingDbContext _context;
    private readonly IUserPermissionService _permissionService;

    public WelcomeController(AccountingDbContext context, IUserPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> Index()
    {
        var branchName = User.FindFirst("CanAccessAllBranches")?.Value == "true" && !User.IsInRole("BranchAdmin")
            ? "All Branches"
            : User.FindFirst("BranchName")?.Value ?? string.Empty;

        var today = DateTime.Today;
        var announcementRows = await _context.Announcements
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => !x.PublishFromDate.HasValue || x.PublishFromDate.Value <= today)
            .Where(x => !x.PublishToDate.HasValue || x.PublishToDate.Value >= today)
            .OrderByDescending(x => x.PublishFromDate ?? x.CreatedDate.Date)
            .ThenByDescending(x => x.AnnouncementId)
            .Take(5)
            .Select(x => new
            {
                AnnouncementId = x.AnnouncementId,
                Title = x.Title,
                Message = x.Message,
                StatusLabel = "Active",
                DateRangeLabel = (x.PublishFromDate.HasValue ? x.PublishFromDate.Value.ToString("dd MMM yyyy") : "Immediate")
                    + " - "
                    + (x.PublishToDate.HasValue ? x.PublishToDate.Value.ToString("dd MMM yyyy") : "Until changed")
            })
            .ToListAsync();

        var announcements = announcementRows
            .Select((x, index) => new WelcomeAnnouncementViewModel
            {
                AnnouncementId = x.AnnouncementId,
                Title = x.Title,
                Message = x.Message,
                StatusLabel = x.StatusLabel,
                DateRangeLabel = x.DateRangeLabel,
                AccentClass = AnnouncementAccentClasses[index % AnnouncementAccentClasses.Length]
            })
            .ToList();

        var model = new WelcomeViewModel
        {
            DisplayName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "User",
            RoleName = User.FindFirstValue(ClaimTypes.Role) ?? "-",
            BranchName = branchName,
            CanUseDashboard = _permissionService.HasMenuAccess(User, "Dashboard.Menu"),
            CanUseFinancialOverview = _permissionService.HasMenuAccess(User, "FinancialOverview.Menu"),
            CanUseInventoryOverview = _permissionService.HasMenuAccess(User, "InventoryOverview.Menu"),
            CanUseAnnouncements = _permissionService.HasMenuAccess(User, "Announcements.Menu"),
            Announcements = announcements
        };

        return View(model);
    }

    public async Task<IActionResult> Announcement(int id)
    {
        var today = DateTime.Today;
        var announcement = await _context.Announcements
            .AsNoTracking()
            .Where(x => x.AnnouncementId == id)
            .Where(x => x.IsActive)
            .Where(x => !x.PublishFromDate.HasValue || x.PublishFromDate.Value <= today)
            .Where(x => !x.PublishToDate.HasValue || x.PublishToDate.Value >= today)
            .Select(x => new WelcomeAnnouncementViewModel
            {
                AnnouncementId = x.AnnouncementId,
                Title = x.Title,
                Message = x.Message,
                StatusLabel = "Active",
                DateRangeLabel = (x.PublishFromDate.HasValue ? x.PublishFromDate.Value.ToString("dd MMM yyyy") : "Immediate")
                    + " - "
                    + (x.PublishToDate.HasValue ? x.PublishToDate.Value.ToString("dd MMM yyyy") : "Until changed"),
                AccentClass = AnnouncementAccentClasses[x.AnnouncementId % AnnouncementAccentClasses.Length]
            })
            .FirstOrDefaultAsync();

        if (announcement is null)
        {
            return NotFound();
        }

        return View("Announcement", announcement);
    }
}
