using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockTransferDetail
{
    public int StockTransferDetailId { get; set; }

    public int StockTransferId { get; set; }

    public int LineNumber { get; set; }

    public int ItemId { get; set; }

    [Range(0.01, 9999999999999999.99)]
    public decimal Qty { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public StockTransferHeader? StockTransferHeader { get; set; }
    public Item? Item { get; set; }
    public ICollection<StockTransferSerial> StockTransferSerials { get; set; } = new List<StockTransferSerial>();
}
