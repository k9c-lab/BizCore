using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockAdjustmentDetail
{
    public int StockAdjustmentDetailId { get; set; }
    public int StockAdjustmentId { get; set; }
    public int LineNumber { get; set; }
    public int ItemId { get; set; }
    public decimal QtyBefore { get; set; }
    public decimal QtyAfter { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public StockAdjustmentHeader? StockAdjustmentHeader { get; set; }
    public Item? Item { get; set; }
}
