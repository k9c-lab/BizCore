namespace BizCore.Models.ViewModels;

public class StockInquiryRowViewModel
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool TrackStock { get; set; }
    public bool IsSerialControlled { get; set; }
    public bool IsActive { get; set; }
}
