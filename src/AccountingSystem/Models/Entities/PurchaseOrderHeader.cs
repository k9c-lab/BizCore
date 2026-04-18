using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PurchaseOrderHeader
{
    public int PurchaseOrderId { get; set; }

    [Required]
    [Display(Name = "PO No.")]
    [StringLength(30)]
    public string PONo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "PO Date")]
    public DateTime PODate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [StringLength(50)]
    [Display(Name = "Reference No.")]
    public string? ReferenceNo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Expected Receive Date")]
    public DateTime? ExpectedReceiveDate { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "VAT Type")]
    public string VatType { get; set; } = "VAT";

    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public Supplier? Supplier { get; set; }
    public ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
    public ICollection<ReceivingHeader> Receivings { get; set; } = new List<ReceivingHeader>();
}
