using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ReceiptHeader
{
    public int ReceiptId { get; set; }

    [Required]
    [Display(Name = "Receipt No.")]
    [StringLength(30)]
    public string ReceiptNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Receipt Date")]
    [DataType(DataType.Date)]
    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    public int PaymentId { get; set; }

    [Display(Name = "Total Received Amount")]
    public decimal TotalReceivedAmount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Issued";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public Customer? Customer { get; set; }
    public PaymentHeader? PaymentHeader { get; set; }
}
