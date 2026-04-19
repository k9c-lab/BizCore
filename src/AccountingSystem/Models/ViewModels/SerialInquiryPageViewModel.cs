namespace BizCore.Models.ViewModels;

public class SerialInquiryPageViewModel
{
    public string? SerialNo { get; set; }
    public string? ItemCode { get; set; }
    public string? PartNumber { get; set; }
    public IReadOnlyList<SerialInquiryRowViewModel> Results { get; set; } = Array.Empty<SerialInquiryRowViewModel>();
}
