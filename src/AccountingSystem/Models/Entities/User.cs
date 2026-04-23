using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class User
{
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Required]
    [StringLength(300)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Role { get; set; } = "Viewer";

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [Display(Name = "All Branches")]
    public bool CanAccessAllBranches { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Branch? Branch { get; set; }
}
