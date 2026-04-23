using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class StockInquirySerialsPageViewModel
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? SerialNo { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();
    public PaginationViewModel Pagination { get; set; } = new();
    public IReadOnlyList<SerialInquiryRowViewModel> Results { get; set; } = Array.Empty<SerialInquiryRowViewModel>();
}
