using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class QuotationFormViewModel
{
    public int? QuotationHeaderId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุเลขที่ใบเสนอราคา")]
    [Display(Name = "เลขที่ใบเสนอราคา")]
    [StringLength(30)]
    public string QuotationNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุวันที่ใบเสนอราคา")]
    [Display(Name = "วันที่ใบเสนอราคา")]
    [DataType(DataType.Date)]
    public DateTime QuotationDate { get; set; } = DateTime.Today;

    [Display(Name = "วันหมดอายุ")]
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

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

    [Display(Name = "เลขที่อ้างอิง")]
    [StringLength(50)]
    public string? ReferenceNo { get; set; }

    [Required(ErrorMessage = "กรุณาระบุสถานะเอกสาร")]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [Required(ErrorMessage = "กรุณาเลือกประเภทภาษี")]
    [Display(Name = "ประเภทภาษี")]
    [StringLength(20)]
    public string VatType { get; set; } = "NoVAT";

    [StringLength(500, ErrorMessage = "หมายเหตุต้องมีความยาวไม่เกิน 500 ตัวอักษร")]
    public string? Remarks { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกรูปแบบส่วนลด")]
    [Display(Name = "รูปแบบส่วนลด")]
    [StringLength(10)]
    public string DiscountMode { get; set; } = "Line";

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "ส่วนลดท้ายเอกสารต้องมากกว่าหรือเท่ากับ 0")]
    public decimal HeaderDiscountAmount { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกประเภทส่วนลดท้ายเอกสาร")]
    [Display(Name = "ประเภทส่วนลดท้ายเอกสาร")]
    [StringLength(10)]
    public string HeaderDiscountType { get; set; } = "Amount";

    [Range(typeof(decimal), "0", "100", ErrorMessage = "เปอร์เซ็นต์ส่วนลดท้ายเอกสารต้องอยู่ระหว่าง 0 ถึง 100")]
    [Display(Name = "ส่วนลดท้ายเอกสาร %")]
    public decimal HeaderDiscountPercent { get; set; }

    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public List<QuotationLineEditorViewModel> Details { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> CustomerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SalespersonOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PriceLevelOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> DiscountModeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> DiscountTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> VatTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
    public IReadOnlyList<QuotationCustomerLookupViewModel> CustomerLookup { get; set; } = Array.Empty<QuotationCustomerLookupViewModel>();
    public IReadOnlyList<QuotationSalespersonLookupViewModel> SalespersonLookup { get; set; } = Array.Empty<QuotationSalespersonLookupViewModel>();
}
