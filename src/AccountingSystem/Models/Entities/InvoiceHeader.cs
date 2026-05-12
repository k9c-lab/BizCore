using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class InvoiceHeader
{
    public int InvoiceId { get; set; }

    [Required]
    [Display(Name = "เลขที่ใบแจ้งหนี้")]
    [StringLength(30)]
    public string InvoiceNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วันที่ใบแจ้งหนี้")]
    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "ลูกค้า")]
    public int CustomerId { get; set; }

    [Display(Name = "พนักงานขาย")]
    public int? SalespersonId { get; set; }

    [Display(Name = "สาขา")]
    public int? BranchId { get; set; }

    [Display(Name = "ระดับราคา")]
    public int? PriceLevelId { get; set; }

    [Display(Name = "ใบเสนอราคา")]
    public int? QuotationId { get; set; }

    [Display(Name = "เลขที่อ้างอิง")]
    [StringLength(50)]
    public string? ReferenceNo { get; set; }

    [Display(Name = "ชื่อ-สกุลคนไข้")]
    [StringLength(200)]
    public string? PatientFullName { get; set; }

    [Display(Name = "อายุ (ปี)")]
    [Range(0, 150)]
    public int? PatientAge { get; set; }

    [Display(Name = "เพศ")]
    [StringLength(20)]
    public string? PatientGender { get; set; }

    [Display(Name = "HN")]
    [StringLength(50)]
    public string? PatientHn { get; set; }

    [Display(Name = "สิทธิการรักษา")]
    public int? TreatmentRightId { get; set; }

    [Display(Name = "Ward")]
    [StringLength(100)]
    public string? PatientWard { get; set; }

    [Display(Name = "แพทย์ส่ง")]
    public int? ReferringDoctorId { get; set; }

    [StringLength(2000)]
    public string? Remark { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }

    [Required]
    [StringLength(10)]
    public string VatType { get; set; } = "NoVAT";

    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public decimal? ReferenceLineSubtotal { get; set; }
    public decimal? ReferenceLineDiscountAmount { get; set; }
    public decimal? ReferenceLineVatAmount { get; set; }
    public decimal? ReferenceLineTotalAmount { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Issued";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public int? IssuedByUserId { get; set; }
    public DateTime? IssuedDate { get; set; }
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledDate { get; set; }

    [StringLength(500)]
    public string? CancelReason { get; set; }

    public Customer? Customer { get; set; }
    public Salesperson? Salesperson { get; set; }
    public Branch? Branch { get; set; }
    public PriceLevel? PriceLevel { get; set; }
    public QuotationHeader? Quotation { get; set; }
    public TreatmentRight? TreatmentRight { get; set; }
    public ReferringDoctor? ReferringDoctor { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? IssuedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    public ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
    public ICollection<BillingNoteInvoice> BillingNoteInvoices { get; set; } = new List<BillingNoteInvoice>();
}
