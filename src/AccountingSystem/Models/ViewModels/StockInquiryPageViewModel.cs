using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class StockInquiryPageViewModel
{
    public string? Search { get; set; }
    public string? ItemType { get; set; }
    public string? Status { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? PartNumber { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();
    public PaginationViewModel Pagination { get; set; } = new();
    public IReadOnlyList<StockInquiryRowViewModel> Results { get; set; } = Array.Empty<StockInquiryRowViewModel>();
}
