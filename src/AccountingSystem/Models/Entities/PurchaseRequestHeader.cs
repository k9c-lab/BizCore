using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PurchaseRequestHeader
{
    public int PurchaseRequestId { get; set; }

    [Required]
    [Display(Name = "PR No.")]
    [StringLength(30)]
    public string PRNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Request Date")]
    public DateTime RequestDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Required Date")]
    public DateTime? RequiredDate { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [StringLength(1000)]
    public string? Purpose { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public int? SubmittedByUserId { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledDate { get; set; }

    [StringLength(500)]
    public string? CancelReason { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public Branch? Branch { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? SubmittedByUser { get; set; }
    public User? ApprovedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<PurchaseRequestDetail> PurchaseRequestDetails { get; set; } = new List<PurchaseRequestDetail>();
    public ICollection<PurchaseOrderHeader> PurchaseOrderHeaders { get; set; } = new List<PurchaseOrderHeader>();
}
