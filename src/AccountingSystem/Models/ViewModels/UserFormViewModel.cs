using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class UserFormViewModel
{
    public int? UserId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Required]
    [StringLength(30)]
    public string Role { get; set; } = "Viewer";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match.")]
    [Display(Name = "Confirm Password")]
    public string? ConfirmPassword { get; set; }

    public bool IsEdit => UserId.HasValue;

    public IEnumerable<SelectListItem> RoleOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}

