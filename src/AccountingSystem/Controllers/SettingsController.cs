using BizCore.Models.ViewModels;
using BizCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : CrudControllerBase
{
    private readonly ISystemSettingService _systemSettingService;
    private readonly IDatabaseMigrationService _databaseMigrationService;

    public SettingsController(
        ISystemSettingService systemSettingService,
        IDatabaseMigrationService databaseMigrationService)
    {
        _systemSettingService = systemSettingService;
        _databaseMigrationService = databaseMigrationService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var pricingMode = await _systemSettingService.GetPricingModeAsync(cancellationToken);
        var migrationStatus = await _databaseMigrationService.GetStatusAsync(cancellationToken);
        return View(await BuildModelAsync(pricingMode, migrationStatus));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PricingSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!PricingModes.All.Contains(model.PricingMode, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.PricingMode), "Please select a valid pricing mode.");
        }

        if (!ModelState.IsValid)
        {
            model.PricingModeOptions = BuildPricingModeOptions(model.PricingMode);
            model.MigrationStatus = await _databaseMigrationService.GetStatusAsync(cancellationToken);
            return View("Index", model);
        }

        await _systemSettingService.SetPricingModeAsync(model.PricingMode, CurrentUserId(), cancellationToken);
        TempData["SettingsNotice"] = "Pricing settings were updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMigrations(CancellationToken cancellationToken)
    {
        var appliedCount = await _databaseMigrationService.RunPendingAsync(CurrentUserId(), cancellationToken);
        TempData["SettingsNotice"] = appliedCount > 0
            ? $"Database upgrade completed successfully. Applied {appliedCount} script(s)."
            : "Database is already up to date. No pending scripts were found.";

        return RedirectToAction(nameof(Index));
    }

    private static Task<PricingSettingsViewModel> BuildModelAsync(string pricingMode, DatabaseMigrationStatusViewModel migrationStatus)
    {
        return Task.FromResult(new PricingSettingsViewModel
        {
            PricingMode = pricingMode,
            PricingModeOptions = BuildPricingModeOptions(pricingMode),
            MigrationStatus = migrationStatus
        });
    }

    private static IReadOnlyList<SelectListItem> BuildPricingModeOptions(string selectedValue)
    {
        return new[]
        {
            new SelectListItem
            {
                Value = PricingModes.SinglePrice,
                Text = "Single Price",
                Selected = string.Equals(selectedValue, PricingModes.SinglePrice, StringComparison.OrdinalIgnoreCase)
            },
            new SelectListItem
            {
                Value = PricingModes.MultiPrice,
                Text = "Multi Price",
                Selected = string.Equals(selectedValue, PricingModes.MultiPrice, StringComparison.OrdinalIgnoreCase)
            }
        };
    }
}
