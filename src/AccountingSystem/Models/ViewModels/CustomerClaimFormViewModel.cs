using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class CustomerClaimFormViewModel
{
    public int? SerialId { get; set; }
    public string CustomerClaimNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Claim Date")]
    public DateTime CustomerClaimDate { get; set; } = DateTime.Today;

    [StringLength(1000)]
    [Display(Name = "Problem Description")]
    public string? ProblemDescription { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public string SerialNo { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
    public string CurrentSerialStatus { get; set; } = string.Empty;
    public DateTime? CustomerWarrantyStartDate { get; set; }
    public DateTime? CustomerWarrantyEndDate { get; set; }
    public bool IsClaimBlocked { get; set; }
    public string ClaimBlockMessage { get; set; } = string.Empty;
}
