using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class SerialClaimLog
{
    public int SerialClaimLogId { get; set; }

    [Required]
    public int SerialId { get; set; }

    [Required]
    public int SupplierId { get; set; }

    public int? CustomerClaimId { get; set; }
    public int? SupplierReplacementSerialId { get; set; }
    public int? BranchId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Claim Date")]
    public DateTime ClaimDate { get; set; } = DateTime.Today;

    [Display(Name = "Problem Description")]
    [StringLength(1000)]
    public string? ProblemDescription { get; set; }

    [Required]
    [Display(Name = "Claim Status")]
    [StringLength(20)]
    public string ClaimStatus { get; set; } = "Open";

    [StringLength(500)]
    public string? Remark { get; set; }

    [StringLength(30)]
    public string? ResultType { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    public DateTime? SentDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? ClosedDate { get; set; }

    public SerialNumber? SerialNumber { get; set; }
    public SerialNumber? SupplierReplacementSerialNumber { get; set; }
    public Supplier? Supplier { get; set; }
    public Branch? Branch { get; set; }
    public CustomerClaimHeader? CustomerClaimHeader { get; set; }
}
