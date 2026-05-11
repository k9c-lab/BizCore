using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class PurchaseRequestLineEditorViewModel
{
    public int? PurchaseRequestDetailId { get; set; }
    public int LineNumber { get; set; }

    [Display(Name = "สินค้า")]
    public int? ItemId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [Display(Name = "จำนวนที่ขอซื้อ")]
    public decimal RequestedQty { get; set; } = 1m;

    [StringLength(500)]
    public string? Remark { get; set; }
}
