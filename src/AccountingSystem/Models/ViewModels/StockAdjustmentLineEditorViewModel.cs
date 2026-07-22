using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class StockAdjustmentLineEditorViewModel
{
    public int LineNumber { get; set; }

    [Display(Name = "สินค้า")]
    public int? ItemId { get; set; }

    [Range(0, 9999999999999999.99)]
    [Display(Name = "จำนวนใหม่")]
    public decimal NewQty { get; set; }

    [StringLength(500)]
    [Display(Name = "หมายเหตุรายการ")]
    public string? Remark { get; set; }
}
