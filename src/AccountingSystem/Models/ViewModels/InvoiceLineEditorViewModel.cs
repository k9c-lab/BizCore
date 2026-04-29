using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class InvoiceLineEditorViewModel
{
    public int? InvoiceDetailId { get; set; }
    public int LineNumber { get; set; }

    [Required(ErrorMessage = "Please select an item.")]
    [Display(Name = "Item")]
    public int? ItemId { get; set; }

    public int? QuotationDetailId { get; set; }

    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Product";
    public bool TrackStock { get; set; }
    public bool IsSerialControlled { get; set; }
    public decimal CurrentStock { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Quoted quantity must be greater than zero.")]
    public decimal QuotedQty { get; set; } = 1m;

    public decimal? PreviouslyInvoicedQty { get; set; }
    public decimal? RemainingQuotedQty { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Qty { get; set; } = 1m;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "Line Total")]
    public decimal LineTotal { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty Start Date")]
    public DateTime? CustomerWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty End Date")]
    public DateTime? CustomerWarrantyEndDate { get; set; }

    public List<int> SelectedSerialIds { get; set; } = new();
}
