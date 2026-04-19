using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Supplier
{
    public int SupplierId { get; set; }

    [Required]
    [Display(Name = "Supplier Code")]
    [StringLength(30)]
    public string SupplierCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Supplier Name")]
    [StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [Display(Name = "Tax ID")]
    [StringLength(30)]
    public string? TaxId { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Phone]
    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Range(0, 9999999999999999.99)]
    public decimal CreditLimit { get; set; }

    public bool IsActive { get; set; } = true;
}
