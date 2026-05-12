using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class AccountChangePasswordViewModel
{
    [Required(ErrorMessage = "กรุณาระบุรหัสผ่านปัจจุบัน")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านปัจจุบัน")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุรหัสผ่านใหม่")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านใหม่ต้องมีความยาวอย่างน้อย 8 ตัวอักษร และไม่เกิน 100 ตัวอักษร")]
    [Display(Name = "รหัสผ่านใหม่")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่านใหม่")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "รหัสผ่านใหม่และการยืนยันรหัสผ่านไม่ตรงกัน")]
    [Display(Name = "ยืนยันรหัสผ่านใหม่")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
