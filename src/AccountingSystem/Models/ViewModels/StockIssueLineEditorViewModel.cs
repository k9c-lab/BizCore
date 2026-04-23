using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class StockIssueLineEditorViewModel
{
    public int? StockIssueDetailId { get; set; }

    public int LineNumber { get; set; }

    [Display(Name = "Item")]
    public int? ItemId { get; set; }

    [Range(0, 9999999999999999.99)]
    [Display(Name = "Qty")]
    public decimal Qty { get; set; }

    [Display(Name = "Serial Numbers")]
    public string? SerialEntryText { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}
