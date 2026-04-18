using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class QuotationHeader
{
    public int QuotationHeaderId { get; set; }

    [Required]
    [Display(Name = "Quotation No.")]
    [StringLength(30)]
    public string QuotationNumber { get; set; } = string.Empty;

    [Display(Name = "Quotation Date")]
    [DataType(DataType.Date)]
    public DateTime QuotationDate { get; set; } = DateTime.Today;

    [Display(Name = "Expiry Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Salesperson")]
    public int? SalespersonId { get; set; }

    [Display(Name = "Reference No.")]
    [StringLength(50)]
    public string? ReferenceNo { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Display(Name = "Subtotal")]
    public decimal Subtotal { get; set; }

    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "Discount Mode")]
    [StringLength(10)]
    public string DiscountMode { get; set; } = "Line";

    [Display(Name = "Header Discount")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal HeaderDiscountAmount { get; set; }

    [Display(Name = "VAT Type")]
    [StringLength(10)]
    public string VatType { get; set; } = "NoVAT";

    [Display(Name = "VAT")]
    public decimal VatAmount { get; set; }

    [Display(Name = "Total")]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public Customer? Customer { get; set; }
    public Salesperson? Salesperson { get; set; }
    public ICollection<QuotationDetail> QuotationDetails { get; set; } = new List<QuotationDetail>();
}
