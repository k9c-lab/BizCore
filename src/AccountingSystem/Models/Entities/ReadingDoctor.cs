using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ReadingDoctor
{
    public int ReadingDoctorId { get; set; }

    [Required]
    [Display(Name = "รหัสแพทย์อ่านผล")]
    [StringLength(30)]
    public string DoctorCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อแพทย์อ่านผล")]
    [StringLength(200)]
    public string DoctorName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
