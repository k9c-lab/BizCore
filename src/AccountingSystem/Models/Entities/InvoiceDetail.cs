using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class InvoiceDetail
{
    public int InvoiceDetailId { get; set; }
    public int InvoiceId { get; set; }
    public int LineNumber { get; set; }

    [Required]
    [Display(Name = "Item")]
    public int ItemId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Qty { get; set; } = 1m;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty Start Date")]
    public DateTime? CustomerWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty End Date")]
    public DateTime? CustomerWarrantyEndDate { get; set; }

    public InvoiceHeader? InvoiceHeader { get; set; }
    public Item? Item { get; set; }
    public ICollection<InvoiceSerial> InvoiceSerials { get; set; } = new List<InvoiceSerial>();
}
