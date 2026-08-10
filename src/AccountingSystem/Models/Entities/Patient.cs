using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Patient
{
    public int PatientId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุ HN")]
    [Display(Name = "HN")]
    [StringLength(30)]
    public string HN { get; set; } = string.Empty;

    [Display(Name = "เลขบัตรประชาชน")]
    [StringLength(20)]
    public string? NationalId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุชื่อ")]
    [Display(Name = "ชื่อ")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุนามสกุล")]
    [Display(Name = "นามสกุล")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "เพศ")]
    [StringLength(10)]
    public string? Gender { get; set; }

    [Display(Name = "วันเกิด")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "โทรศัพท์")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "ที่อยู่")]
    [StringLength(500)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PatientVisit> PatientVisits { get; set; } = new List<PatientVisit>();
}
