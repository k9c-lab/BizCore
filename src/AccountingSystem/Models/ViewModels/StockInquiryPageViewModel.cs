namespace BizCore.Models.ViewModels;

public class StockInquiryPageViewModel
{
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? PartNumber { get; set; }
    public IReadOnlyList<StockInquiryRowViewModel> Results { get; set; } = Array.Empty<StockInquiryRowViewModel>();
}
