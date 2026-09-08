using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class PatientVisitFormViewModel
{
    public int? PatientVisitId { get; set; }

    [Display(Name = "เลขที่ลงทะเบียน (VN)")]
    public string VN { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุวันที่ลงทะเบียน")]
    [Display(Name = "วันที่ลงทะเบียน")]
    [DataType(DataType.Date)]
    public DateTime VisitDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาเลือกคนไข้")]
    [Display(Name = "คนไข้")]
    public int? PatientId { get; set; }

    [Display(Name = "ลูกค้า (ผู้ชำระ)")]
    public int? CustomerId { get; set; }

    [Display(Name = "น้ำหนัก (กก.)")]
    public decimal? Weight { get; set; }

    [Display(Name = "ส่วนสูง (ซม.)")]
    public decimal? Height { get; set; }

    [Display(Name = "Ward")]
    [StringLength(100)]
    public string? Ward { get; set; }

    [Display(Name = "สิทธิการรักษา")]
    public int? TreatmentRightId { get; set; }

    [Display(Name = "แพทย์ส่ง")]
    public int? ReferringDoctorId { get; set; }

    [Display(Name = "โรงพยาบาลส่งตัว")]
    [StringLength(200)]
    public string? ReferringHospital { get; set; }

    [Display(Name = "หมายเหตุ")]
    [StringLength(2000)]
    public string? Remark { get; set; }

    public string Status { get; set; } = "Registered";
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    // Inline new patient creation
    public bool IsNewPatient { get; set; }
    public string? UpdatePatientPrefix { get; set; }
    [Display(Name = "HN")]
    [StringLength(30)]
    public string? NewPatientHN { get; set; }
    [Display(Name = "เลขบัตร ปชช.")]
    [StringLength(20)]
    public string? NewPatientNationalId { get; set; }
    [Display(Name = "คำนำหน้า")]
    [StringLength(20)]
    public string? NewPatientNamePrefix { get; set; }
    [Display(Name = "ชื่อ")]
    [StringLength(100)]
    public string? NewPatientFirstName { get; set; }
    [Display(Name = "นามสกุล")]
    [StringLength(100)]
    public string? NewPatientLastName { get; set; }
    [Display(Name = "เพศ")]
    public string? NewPatientGender { get; set; }
    [Display(Name = "วันเกิด")]
    [DataType(DataType.Date)]
    public DateTime? NewPatientDateOfBirth { get; set; }
    [Display(Name = "โทรศัพท์")]
    [StringLength(20)]
    public string? NewPatientPhone { get; set; }

    public List<PatientVisitItemViewModel> Items { get; set; } = new();

    // Edit mode: linked invoice info (for sync confirmation)
    public int? InvoiceId { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvoiceStatus { get; set; }
    public bool SyncInvoice { get; set; }

    // Edit mode: editable patient master fields
    public string? EditPatientHN { get; set; }
    [Display(Name = "คำนำหน้า")]
    [StringLength(20)]
    public string? EditPatientNamePrefix { get; set; }
    [Display(Name = "ชื่อ")]
    [StringLength(100)]
    public string? EditPatientFirstName { get; set; }
    [Display(Name = "นามสกุล")]
    [StringLength(100)]
    public string? EditPatientLastName { get; set; }
    [Display(Name = "วันเกิด")]
    [DataType(DataType.Date)]
    public DateTime? EditPatientDateOfBirth { get; set; }
    [Display(Name = "เพศ")]
    public string? EditPatientGender { get; set; }
    [Display(Name = "โทรศัพท์")]
    [StringLength(20)]
    public string? EditPatientPhone { get; set; }

    // Edit mode: audit log display
    public IReadOnlyList<PatientVisitAuditLogViewModel> AuditLogs { get; set; } = Array.Empty<PatientVisitAuditLogViewModel>();

    public IEnumerable<SelectListItem> TreatmentRightOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ReferringDoctorOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<PatientVisitItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<PatientVisitItemLookupViewModel>();
    public IReadOnlyList<PatientLookupViewModel> PatientLookup { get; set; } = Array.Empty<PatientLookupViewModel>();
    public IReadOnlyList<QuotationCustomerLookupViewModel> CustomerLookup { get; set; } = Array.Empty<QuotationCustomerLookupViewModel>();
}

public class PatientVisitAuditLogViewModel
{
    public string UserName { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PatientVisitItemViewModel
{
    public int? PatientVisitItemId { get; set; }
    public int? ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    [Range(0.01, 9999999, ErrorMessage = "จำนวนต้องมากกว่า 0")]
    [Display(Name = "จำนวน")]
    public decimal Quantity { get; set; } = 1;
}

public class PatientLookupViewModel
{
    public int PatientId { get; set; }
    public string HN { get; set; } = string.Empty;
    public string? NamePrefix { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }
}

public class PatientVisitItemLookupViewModel
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
