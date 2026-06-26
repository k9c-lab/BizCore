using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class PricingSettingsViewModel
{
    [Required]
    [Display(Name = "Pricing Mode")]
    public string PricingMode { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> PricingModeOptions { get; set; } = Array.Empty<SelectListItem>();

    [Display(Name = "Show Patient Information")]
    public bool EnablePatientInfo { get; set; } = true;

    public DatabaseMigrationStatusViewModel MigrationStatus { get; set; } = new();
}
