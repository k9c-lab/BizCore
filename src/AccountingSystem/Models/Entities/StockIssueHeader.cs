using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockIssueHeader
{
    public int StockIssueId { get; set; }

    [Required]
    [Display(Name = "Issue No.")]
    [StringLength(30)]
    public string IssueNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Issue Date")]
    public DateTime IssueDate { get; set; } = DateTime.Today;

    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required]
    [Display(Name = "Issue Type")]
    [StringLength(30)]
    public string IssueType { get; set; } = "InternalUse";

    [StringLength(500)]
    public string? Purpose { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(500)]
    public string? Remark { get; set; }

    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public int? PostedByUserId { get; set; }
    public DateTime? PostedDate { get; set; }
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledDate { get; set; }

    [StringLength(500)]
    public string? CancelReason { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public Branch? Branch { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? PostedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<StockIssueDetail> StockIssueDetails { get; set; } = new List<StockIssueDetail>();
}
