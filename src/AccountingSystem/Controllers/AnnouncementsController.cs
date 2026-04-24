using BizCore.Data;
using BizCore.Models.Entities;
using BizCore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Controllers;

[Authorize]
public class AnnouncementsController : CrudControllerBase
{
    private readonly AccountingDbContext _context;

    public AnnouncementsController(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        var query = _context.Announcements
            .AsNoTracking()
            .Include(x => x.CreatedByUser)
            .Include(x => x.UpdatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x => x.Title.Contains(keyword) || x.Message.Contains(keyword));
        }

        var today = DateTime.Today;
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status switch
            {
                "Active" => query.Where(x => x.IsActive && (!x.PublishFromDate.HasValue || x.PublishFromDate.Value <= today) && (!x.PublishToDate.HasValue || x.PublishToDate.Value >= today)),
                "Scheduled" => query.Where(x => x.IsActive && x.PublishFromDate.HasValue && x.PublishFromDate.Value > today),
                "Expired" => query.Where(x => x.IsActive && x.PublishToDate.HasValue && x.PublishToDate.Value < today),
                "Inactive" => query.Where(x => !x.IsActive),
                _ => query
            };
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var announcements = await PaginatedList<Announcement>.CreateAsync(
            query.OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.PublishFromDate ?? x.CreatedDate)
                .ThenByDescending(x => x.AnnouncementId),
            page,
            pageSize);

        return View(announcements);
    }

    public IActionResult Create()
    {
        return View(new Announcement
        {
            PublishFromDate = DateTime.Today,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Announcement model)
    {
        if (!ValidateAnnouncement(model))
        {
            return View(model);
        }

        var now = DateTime.UtcNow;
        model.CreatedDate = now;
        model.UpdatedDate = now;
        model.CreatedByUserId = CurrentUserId();
        model.UpdatedByUserId = CurrentUserId();
        model.Title = model.Title.Trim();
        model.Message = model.Message.Trim();

        _context.Announcements.Add(model);
        await _context.SaveChangesAsync();

        TempData["AnnouncementNotice"] = "Announcement was saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var announcement = await _context.Announcements.FindAsync(id.Value);
        return announcement is null ? NotFound() : View(announcement);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Announcement model)
    {
        if (id != model.AnnouncementId)
        {
            return NotFound();
        }

        var announcement = await _context.Announcements.FirstOrDefaultAsync(x => x.AnnouncementId == id);
        if (announcement is null)
        {
            return NotFound();
        }

        if (!ValidateAnnouncement(model))
        {
            return View(model);
        }

        announcement.Title = model.Title.Trim();
        announcement.Message = model.Message.Trim();
        announcement.PublishFromDate = model.PublishFromDate;
        announcement.PublishToDate = model.PublishToDate;
        announcement.IsActive = model.IsActive;
        announcement.UpdatedDate = DateTime.UtcNow;
        announcement.UpdatedByUserId = CurrentUserId();

        await _context.SaveChangesAsync();

        TempData["AnnouncementNotice"] = "Announcement was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var announcement = await _context.Announcements.FirstOrDefaultAsync(x => x.AnnouncementId == id);
        if (announcement is null)
        {
            return NotFound();
        }

        announcement.IsActive = !announcement.IsActive;
        announcement.UpdatedDate = DateTime.UtcNow;
        announcement.UpdatedByUserId = CurrentUserId();
        await _context.SaveChangesAsync();

        TempData["AnnouncementNotice"] = announcement.IsActive
            ? "Announcement is now active."
            : "Announcement is now inactive.";
        return RedirectToAction(nameof(Index));
    }

    private bool ValidateAnnouncement(Announcement model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError(nameof(Announcement.Title), "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(model.Message))
        {
            ModelState.AddModelError(nameof(Announcement.Message), "Message is required.");
        }

        if (model.PublishFromDate.HasValue && model.PublishToDate.HasValue && model.PublishToDate.Value.Date < model.PublishFromDate.Value.Date)
        {
            ModelState.AddModelError(nameof(Announcement.PublishToDate), "Publish To must be on or after Publish From.");
        }

        return ModelState.IsValid;
    }
}
