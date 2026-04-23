using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ReceivingHeader
{
    public int ReceivingId { get; set; }

    [Required]
    [Display(Name = "Receiving No.")]
    [StringLength(30)]
    public string ReceivingNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Receive Date")]
    public DateTime ReceiveDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Required]
    [Display(Name = "Purchase Order")]
    public int PurchaseOrderId { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [StringLength(50)]
    [Display(Name = "Delivery Note No.")]
    public string? DeliveryNoteNo { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Posted";

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

    public Supplier? Supplier { get; set; }
    public PurchaseOrderHeader? PurchaseOrderHeader { get; set; }
    public Branch? Branch { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? PostedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<ReceivingDetail> ReceivingDetails { get; set; } = new List<ReceivingDetail>();
}
