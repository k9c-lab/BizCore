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

    [Display(Name = "ชื่อผู้มีอำนาจลงนาม")]
    [StringLength(200)]
    public string AuthorisedName { get; set; } = string.Empty;

    [Display(Name = "ตำแหน่ง")]
    [StringLength(100)]
    public string AuthorisedTitle { get; set; } = string.Empty;

    public DatabaseMigrationStatusViewModel MigrationStatus { get; set; } = new();
}
