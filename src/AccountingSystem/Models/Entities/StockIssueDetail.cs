using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockIssueDetail
{
    public int StockIssueDetailId { get; set; }

    public int StockIssueId { get; set; }

    public int LineNumber { get; set; }

    public int ItemId { get; set; }

    [Range(0.01, 9999999999999999.99)]
    public decimal Qty { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public StockIssueHeader? StockIssueHeader { get; set; }
    public Item? Item { get; set; }
    public ICollection<StockIssueSerial> StockIssueSerials { get; set; } = new List<StockIssueSerial>();
}
