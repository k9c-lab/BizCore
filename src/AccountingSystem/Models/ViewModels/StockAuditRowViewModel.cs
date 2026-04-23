namespace BizCore.Models.ViewModels;

public class StockAuditRowViewModel
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public bool IsSerialControlled { get; set; }
    public decimal BalanceQty { get; set; }
    public decimal? SerialInStockQty { get; set; }
    public decimal LedgerNetQty { get; set; }
    public decimal BalanceLedgerDiff { get; set; }
    public decimal? BalanceSerialDiff { get; set; }
    public bool IsMismatch { get; set; }
    public string StatusText => IsMismatch ? "Mismatch" : "OK";
    public string IssueText { get; set; } = string.Empty;
}
