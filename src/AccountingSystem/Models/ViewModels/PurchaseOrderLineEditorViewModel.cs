using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class PurchaseOrderLineEditorViewModel
{
    public int? PurchaseOrderDetailId { get; set; }
    public int LineNumber { get; set; }

    [Required(ErrorMessage = "Please select an item.")]
    public int? ItemId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Qty { get; set; } = 1m;

    public decimal ReceivedQty { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }

    [StringLength(300)]
    public string? Remark { get; set; }

    public List<PurchaseOrderAllocationEditorViewModel> Allocations { get; set; } = new();
}
