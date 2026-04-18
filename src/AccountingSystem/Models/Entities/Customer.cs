using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Customer
{
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Customer Code")]
    [StringLength(30)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Customer Name")]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

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
