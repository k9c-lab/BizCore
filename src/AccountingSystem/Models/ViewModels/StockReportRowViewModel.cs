namespace BizCore.Models.ViewModels;

public class StockReportRowViewModel
{
    public string BranchName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public decimal QtyOnHand { get; set; }
}
