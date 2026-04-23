using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class SerialInquiryPageViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public string? SerialNo { get; set; }
    public string? ItemCode { get; set; }
    public string? PartNumber { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();
    public PaginationViewModel Pagination { get; set; } = new();
    public IReadOnlyList<SerialInquiryRowViewModel> Results { get; set; } = Array.Empty<SerialInquiryRowViewModel>();
}
