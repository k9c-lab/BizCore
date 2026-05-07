using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ReferringDoctor
{
    public int ReferringDoctorId { get; set; }

    [Required]
    [Display(Name = "รหัสแพทย์ส่ง")]
    [StringLength(30)]
    public string DoctorCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อแพทย์ส่ง")]
    [StringLength(200)]
    public string DoctorName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
