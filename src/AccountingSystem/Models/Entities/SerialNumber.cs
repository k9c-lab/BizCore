using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class SerialNumber
{
    public int SerialId { get; set; }

    [Required]
    public int ItemId { get; set; }

    [Required]
    [Display(Name = "Serial No.")]
    [StringLength(120)]
    public string SerialNo { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Available";

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Current Customer")]
    public int? CurrentCustomerId { get; set; }

    public int? InvoiceId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Supplier Warranty Start Date")]
    public DateTime? SupplierWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Supplier Warranty End Date")]
    public DateTime? SupplierWarrantyEndDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty Start Date")]
    public DateTime? CustomerWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty End Date")]
    public DateTime? CustomerWarrantyEndDate { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Item? Item { get; set; }
    public Supplier? Supplier { get; set; }
    public Customer? CurrentCustomer { get; set; }
    public InvoiceHeader? InvoiceHeader { get; set; }
}
