using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class QuotationLineEditorViewModel
{
    public int? QuotationDetailId { get; set; }

    public int LineNumber { get; set; }

    [Required(ErrorMessage = "Please select an item.")]
    [Display(Name = "Item")]
    public int? ItemId { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Quantity must be greater than zero.")]
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

    [Range(typeof(decimal), "0", "100")]
    [Display(Name = "Discount %")]
    public decimal DiscountPercent { get; set; }

    [Display(Name = "Line Total")]
    public decimal LineTotal { get; set; }

    public decimal NetUnitPrice => Quantity > 0 ? Math.Round(LineTotal / Quantity, 2, MidpointRounding.AwayFromZero) : 0m;
}
