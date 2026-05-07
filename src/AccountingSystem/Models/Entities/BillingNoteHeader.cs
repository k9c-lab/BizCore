using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class BillingNoteHeader
{
    public int BillingNoteId { get; set; }

    [Required]
    [Display(Name = "เลขที่ใบวางบิล")]
    [StringLength(30)]
    public string BillingNoteNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วันที่ใบวางบิล")]
    [DataType(DataType.Date)]
    public DateTime BillingNoteDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "ลูกค้า")]
    public int CustomerId { get; set; }

    [Display(Name = "สาขา")]
    public int? BranchId { get; set; }

    [Required]
    [Display(Name = "รูปแบบสรุป")]
    [StringLength(30)]
    public string SummaryMode { get; set; } = "TreatmentRight";

    [Display(Name = "จำนวนใบแจ้งหนี้")]
    public int InvoiceCount { get; set; }

    [Display(Name = "ยอดก่อน VAT")]
    public decimal SubtotalAmount { get; set; }

    [Display(Name = "ส่วนลด")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "VAT")]
    public decimal VatAmount { get; set; }

    [Display(Name = "ยอดรวม")]
    public decimal TotalAmount { get; set; }

    [Display(Name = "ชำระแล้ว")]
    public decimal PaidAmount { get; set; }

    [Display(Name = "ยอดคงเหลือ")]
    public decimal BalanceAmount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Issued";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledDate { get; set; }

    [StringLength(500)]
    public string? CancelReason { get; set; }

    public Customer? Customer { get; set; }
    public Branch? Branch { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<BillingNoteInvoice> BillingNoteInvoices { get; set; } = new List<BillingNoteInvoice>();
    public ICollection<BillingNoteLine> BillingNoteLines { get; set; } = new List<BillingNoteLine>();
}
