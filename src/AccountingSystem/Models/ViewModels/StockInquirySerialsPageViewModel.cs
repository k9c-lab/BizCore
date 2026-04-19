namespace BizCore.Models.ViewModels;

public class StockInquirySerialsPageViewModel
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public string? SerialNo { get; set; }
    public IReadOnlyList<SerialInquiryRowViewModel> Results { get; set; } = Array.Empty<SerialInquiryRowViewModel>();
}
