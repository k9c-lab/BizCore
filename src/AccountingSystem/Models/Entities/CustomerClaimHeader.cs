using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class CustomerClaimHeader
{
    public int CustomerClaimId { get; set; }

    [Required]
    [Display(Name = "Claim No.")]
    [StringLength(30)]
    public string CustomerClaimNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Claim Date")]
    public DateTime CustomerClaimDate { get; set; } = DateTime.Today;

    [Required]
    public int CustomerId { get; set; }

    public int? InvoiceId { get; set; }

    public int? BranchId { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Open";

    [Display(Name = "Problem Description")]
    [StringLength(1000)]
    public string? ProblemDescription { get; set; }

    [StringLength(1000)]
    public string? ResolutionRemark { get; set; }

    [StringLength(500)]
    public string? CancelReason { get; set; }

    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public int? ReceivedByUserId { get; set; }
    public int? SentToSupplierByUserId { get; set; }
    public int? ResolvedByUserId { get; set; }
    public int? ReturnedByUserId { get; set; }
    public int? ClosedByUserId { get; set; }
    public int? CancelledByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? SentToSupplierDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public DateTime? CancelledDate { get; set; }

    public Customer? Customer { get; set; }
    public InvoiceHeader? InvoiceHeader { get; set; }
    public Branch? Branch { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? ReceivedByUser { get; set; }
    public User? SentToSupplierByUser { get; set; }
    public User? ResolvedByUser { get; set; }
    public User? ReturnedByUser { get; set; }
    public User? ClosedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<CustomerClaimDetail> CustomerClaimDetails { get; set; } = new List<CustomerClaimDetail>();
}
