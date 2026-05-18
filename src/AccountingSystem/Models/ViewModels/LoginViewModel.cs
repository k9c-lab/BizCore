using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [StringLength(50)]
    [Display(Name = "ชื่อผู้ใช้")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "ชื่อผู้ใช้ใช้ได้เฉพาะตัวอักษรภาษาอังกฤษ ตัวเลข จุด (.) ขีดล่าง (_) และขีดกลาง (-)")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [Display(Name = "รหัสผ่าน")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "จดจำการเข้าสู่ระบบ")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
