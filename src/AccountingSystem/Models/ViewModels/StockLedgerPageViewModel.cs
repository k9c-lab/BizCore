using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class StockLedgerPageViewModel
{
    public string? Search { get; set; }
    public int? ItemId { get; set; }
    public int? BranchId { get; set; }
    public string? MovementType { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public decimal IssueQty { get; set; }
    public decimal IssueCancelQty { get; set; }
    public decimal NetIssuedQty { get; set; }
    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ItemOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> MovementTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ReferenceTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public PaginationViewModel Pagination { get; set; } = new();
    public IReadOnlyList<StockLedgerRowViewModel> Results { get; set; } = Array.Empty<StockLedgerRowViewModel>();
}
