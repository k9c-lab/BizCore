using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockTransferHeader
{
    public int StockTransferId { get; set; }

    [Required]
    [Display(Name = "Transfer No.")]
    [StringLength(30)]
    public string TransferNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Transfer Date")]
    public DateTime TransferDate { get; set; } = DateTime.Today;

    [Display(Name = "From Branch")]
    public int FromBranchId { get; set; }

    [Display(Name = "To Branch")]
    public int ToBranchId { get; set; }

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

    public Branch? FromBranch { get; set; }
    public Branch? ToBranch { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? PostedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<StockTransferDetail> StockTransferDetails { get; set; } = new List<StockTransferDetail>();
}
