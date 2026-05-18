using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class InvoiceFormViewModel
{
    public int? InvoiceId { get; set; }
    public int? QuotationId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุเลขที่ใบแจ้งหนี้")]
    [Display(Name = "เลขที่ใบแจ้งหนี้")]
    [StringLength(30)]
    public string InvoiceNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุวันที่ใบแจ้งหนี้")]
    [Display(Name = "วันที่ใบแจ้งหนี้")]
    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาเลือกลูกค้า")]
    [Display(Name = "ลูกค้า")]
    public int? CustomerId { get; set; }

    [Display(Name = "พนักงานขาย")]
    public int? SalespersonId { get; set; }

    [Display(Name = "สาขา")]
    public int? BranchId { get; set; }

    [Display(Name = "ระดับราคา")]
    public int? PriceLevelId { get; set; }

    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public string PricingMode { get; set; } = string.Empty;
    public bool ShowPriceLevelSelector { get; set; }
    public bool ShowPatientInfo { get; set; } = true;

    [Display(Name = "เลขที่ใบเสนอราคา (ถ้ามี)")]
    public string? QuotationNo { get; set; }

    [Display(Name = "เลขที่อ้างอิง")]
    [StringLength(50)]
    public string? ReferenceNo { get; set; }

    [Display(Name = "ชื่อ-สกุลคนไข้")]
    [StringLength(200)]
    public string? PatientFullName { get; set; }

    [Display(Name = "วันเกิด")]
    [DataType(DataType.Date)]
    public DateTime? PatientBirthDate { get; set; }

    [Display(Name = "อายุ (ปี)")]
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

    [Display(Name = "แพทย์อ่านผล")]
    public int? ReadingDoctorId { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกประเภทภาษี")]
    [Display(Name = "ประเภทภาษี")]
    [StringLength(20)]
    public string VatType { get; set; } = "VATExclusive";

    [Required(ErrorMessage = "กรุณาเลือกรูปแบบส่วนลด")]
    [Display(Name = "รูปแบบส่วนลด")]
    [StringLength(10)]
    public string DiscountMode { get; set; } = "Line";

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "ส่วนลดท้ายเอกสารต้องมากกว่าหรือเท่ากับ 0")]
    [Display(Name = "ส่วนลดท้ายเอกสาร")]
    public decimal HeaderDiscountAmount { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "ยอดเรียกเก็บต้องมากกว่า 0")]
    [Display(Name = "ยอดเรียกเก็บใบนี้")]
    public decimal? AmountDueThisInvoice { get; set; }

    [StringLength(2000, ErrorMessage = "หมายเหตุต้องมีความยาวไม่เกิน 2000 ตัวอักษร")]
    public string? Remark { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReferenceSubtotal { get; set; }
    public decimal ReferenceDiscountAmount { get; set; }
    public decimal ReferenceVatAmount { get; set; }
    public decimal ReferenceTotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = "Issued";

    public List<InvoiceLineEditorViewModel> Details { get; set; } = new();

    public IEnumerable<SelectListItem> CustomerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SalespersonOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PriceLevelOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> QuotationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PatientGenderOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> TreatmentRightOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ReferringDoctorOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ReadingDoctorOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> DiscountModeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> VatTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<InvoiceItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<InvoiceItemLookupViewModel>();
    public IReadOnlyList<InvoiceSerialLookupViewModel> SerialLookup { get; set; } = Array.Empty<InvoiceSerialLookupViewModel>();
    public IReadOnlyList<QuotationCustomerLookupViewModel> CustomerLookup { get; set; } = Array.Empty<QuotationCustomerLookupViewModel>();
    public IReadOnlyList<QuotationSalespersonLookupViewModel> SalespersonLookup { get; set; } = Array.Empty<QuotationSalespersonLookupViewModel>();
}
