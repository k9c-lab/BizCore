using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Salesperson
{
    public int SalespersonId { get; set; }

    [Required]
    [Display(Name = "Salesperson Code")]
    [StringLength(30)]
    public string SalespersonCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Salesperson Name")]
    [StringLength(200)]
    public string SalespersonName { get; set; } = string.Empty;

    [Phone]
    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Range(0, 100)]
    [Display(Name = "Commission Rate (%)")]
    public decimal CommissionRate { get; set; }

    public bool IsActive { get; set; } = true;
}
