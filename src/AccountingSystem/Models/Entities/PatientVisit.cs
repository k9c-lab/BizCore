using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PatientVisit
{
    public int PatientVisitId { get; set; }

    [Required]
    [Display(Name = "เลขที่ลงทะเบียน (VN)")]
    [StringLength(30)]
    public string VN { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วันที่ลงทะเบียน")]
    [DataType(DataType.Date)]
    public DateTime VisitDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "คนไข้")]
    public int PatientId { get; set; }

    [Display(Name = "ลูกค้า (ผู้ชำระ)")]
    public int? CustomerId { get; set; }

    [Display(Name = "น้ำหนัก (กก.)")]
    [Range(0, 999.99)]
    public decimal? Weight { get; set; }

    [Display(Name = "ส่วนสูง (ซม.)")]
    [Range(0, 300)]
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

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Registered";

    public int? InvoiceId { get; set; }
    public int? BranchId { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient? Patient { get; set; }
    public Customer? Customer { get; set; }
    public TreatmentRight? TreatmentRight { get; set; }
    public ReferringDoctor? ReferringDoctor { get; set; }
    public InvoiceHeader? Invoice { get; set; }
    public Branch? Branch { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<PatientVisitItem> PatientVisitItems { get; set; } = new List<PatientVisitItem>();
    public ICollection<PatientVisitAuditLog> AuditLogs { get; set; } = new List<PatientVisitAuditLog>();
}
