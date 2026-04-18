using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class SupplierClaimFormViewModel
{
    [Required]
    public int? SerialId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Claim Date")]
    public DateTime ClaimDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Claim Status")]
    [StringLength(20)]
    public string ClaimStatus { get; set; } = "Open";

    [Display(Name = "Problem Description")]
    [StringLength(1000)]
    public string? ProblemDescription { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public string SerialNo { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string CurrentSerialStatus { get; set; } = string.Empty;
    public DateTime? SupplierWarrantyStartDate { get; set; }
    public DateTime? SupplierWarrantyEndDate { get; set; }

    public bool IsClaimBlocked { get; set; }
    public string ClaimBlockMessage { get; set; } = string.Empty;
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
