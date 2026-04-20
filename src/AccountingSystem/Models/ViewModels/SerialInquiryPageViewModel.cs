namespace BizCore.Models.ViewModels;

public class SerialInquiryPageViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? SerialNo { get; set; }
    public string? ItemCode { get; set; }
    public string? PartNumber { get; set; }
    public PaginationViewModel Pagination { get; set; } = new();
    public IReadOnlyList<SerialInquiryRowViewModel> Results { get; set; } = Array.Empty<SerialInquiryRowViewModel>();
}
