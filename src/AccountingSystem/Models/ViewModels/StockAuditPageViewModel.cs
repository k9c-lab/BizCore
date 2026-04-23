using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class StockAuditPageViewModel
{
    public string? Search { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? AuditStatus { get; set; }
    public bool CanAccessAllBranches { get; set; }
    public int TotalRows { get; set; }
    public int OkRows { get; set; }
    public int MismatchRows { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();
    public PaginationViewModel Pagination { get; set; } = new();
    public IReadOnlyList<StockAuditRowViewModel> Results { get; set; } = Array.Empty<StockAuditRowViewModel>();
}
