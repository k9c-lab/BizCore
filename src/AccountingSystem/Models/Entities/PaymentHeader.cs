using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PaymentHeader
{
    public int PaymentId { get; set; }

    [Required]
    [Display(Name = "Payment No.")]
    [StringLength(30)]
    public string PaymentNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Transfer";

    [Display(Name = "Reference No.")]
    [StringLength(100)]
    public string? ReferenceNo { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Posted";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public int? PostedByUserId { get; set; }
    public DateTime? PostedDate { get; set; }
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledDate { get; set; }

    [StringLength(500)]
    public string? CancelReason { get; set; }

    public Customer? Customer { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? PostedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ReceiptHeader? ReceiptHeader { get; set; }
    public ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
}
