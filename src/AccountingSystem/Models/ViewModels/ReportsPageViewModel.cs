using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class ReportsPageViewModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();
    public decimal SalesTotal { get; set; }
    public decimal PaymentsTotal { get; set; }
    public decimal OutstandingAr { get; set; }
    public decimal StockQty { get; set; }
    public IReadOnlyList<SalesReportRowViewModel> SalesRows { get; set; } = Array.Empty<SalesReportRowViewModel>();
    public IReadOnlyList<StockReportRowViewModel> StockRows { get; set; } = Array.Empty<StockReportRowViewModel>();
    public IReadOnlyList<MovementReportRowViewModel> MovementRows { get; set; } = Array.Empty<MovementReportRowViewModel>();
    public IReadOnlyList<ClaimReportRowViewModel> ClaimRows { get; set; } = Array.Empty<ClaimReportRowViewModel>();
}
