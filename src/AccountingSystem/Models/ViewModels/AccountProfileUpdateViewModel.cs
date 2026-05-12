using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class AccountProfileUpdateViewModel
{
    [Required(ErrorMessage = "กรุณาระบุชื่อที่แสดง")]
    [StringLength(150, ErrorMessage = "ชื่อที่แสดงต้องมีความยาวไม่เกิน 150 ตัวอักษร")]
    [Display(Name = "ชื่อที่แสดง")]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(256, ErrorMessage = "อีเมลต้องมีความยาวไม่เกิน 256 ตัวอักษร")]
    [Display(Name = "อีเมล")]
    public string? Email { get; set; }
}
