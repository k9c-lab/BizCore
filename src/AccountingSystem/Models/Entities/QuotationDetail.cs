using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class QuotationDetail
{
    public int QuotationDetailId { get; set; }

    public int QuotationHeaderId { get; set; }

    public int LineNumber { get; set; }

    [Required]
    [Display(Name = "Item")]
    public int ItemId { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Quantity { get; set; } = 1m;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "Discount Type")]
    [StringLength(10)]
    public string DiscountType { get; set; } = "Amount";

    [Display(Name = "Discount %")]
    [Range(typeof(decimal), "0", "100")]
    public decimal DiscountPercent { get; set; }

    [Display(Name = "Line Total")]
    public decimal LineTotal { get; set; }

    public QuotationHeader? QuotationHeader { get; set; }
    public Item? Item { get; set; }
}
