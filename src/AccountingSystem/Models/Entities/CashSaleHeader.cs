using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class CashSaleHeader
{
    public int CashSaleId { get; set; }

    [Required]
    [Display(Name = "Cash Sale No.")]
    [StringLength(30)]
    public string CashSaleNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Cash Sale Date")]
    [DataType(DataType.Date)]
    public DateTime CashSaleDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Salesperson")]
    public int? SalespersonId { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [Display(Name = "Price Level")]
    public int? PriceLevelId { get; set; }

    [Display(Name = "Reference No.")]
    [StringLength(50)]
    public string? ReferenceNo { get; set; }

    [Display(Name = "Patient Full Name")]
    [StringLength(200)]
    public string? PatientFullName { get; set; }

    [Display(Name = "Patient Age")]
    [Range(0, 150)]
    public int? PatientAge { get; set; }

    [Display(Name = "Gender")]
    [StringLength(20)]
    public string? PatientGender { get; set; }

    [Display(Name = "HN")]
    [StringLength(50)]
    public string? PatientHn { get; set; }

    [Display(Name = "Treatment Right")]
    public int? TreatmentRightId { get; set; }

    [Display(Name = "Ward")]
    [StringLength(100)]
    public string? PatientWard { get; set; }

    [Display(Name = "Referring Doctor")]
    public int? ReferringDoctorId { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }

    [Required]
    [StringLength(10)]
    public string VatType { get; set; } = "VAT";

    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

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
    public TreatmentRight? TreatmentRight { get; set; }
    public ReferringDoctor? ReferringDoctor { get; set; }
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
    public User? IssuedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public ICollection<CashSaleDetail> CashSaleDetails { get; set; } = new List<CashSaleDetail>();
}
