using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Branch
{
    public int BranchId { get; set; }

    [Required]
    [Display(Name = "Branch Code")]
    [StringLength(30)]
    public string BranchCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Branch Name")]
    [StringLength(150)]
    public string BranchName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Phone")]
    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
