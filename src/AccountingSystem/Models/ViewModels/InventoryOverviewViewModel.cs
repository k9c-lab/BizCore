using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class InventoryOverviewViewModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();

    public decimal StockOnHandQty { get; set; }
    public int InventoryItems { get; set; }
    public int LowStockItems { get; set; }
    public int SerialsInStock { get; set; }
    public int PendingReceivings { get; set; }
    public int OpenClaims { get; set; }

    public IReadOnlyList<StockReportRowViewModel> TopStockRows { get; set; } = Array.Empty<StockReportRowViewModel>();
    public IReadOnlyList<StockReportRowViewModel> LowStockRows { get; set; } = Array.Empty<StockReportRowViewModel>();
    public IReadOnlyList<MovementReportRowViewModel> MovementRows { get; set; } = Array.Empty<MovementReportRowViewModel>();
    public IReadOnlyList<PartyBalanceRowViewModel> BranchStockRows { get; set; } = Array.Empty<PartyBalanceRowViewModel>();
}
