namespace BizCore.Models.ViewModels;

public class DashboardViewModel
{
    public string BranchName { get; set; } = string.Empty;
    public DateTime Today { get; set; } = DateTime.Today;
    public decimal SalesToday { get; set; }
    public decimal SalesThisMonth { get; set; }
    public decimal PaymentsThisMonth { get; set; }
    public decimal OutstandingAr { get; set; }
    public decimal StockOnHandQty { get; set; }
    public int LowStockItems { get; set; }
    public int OpenPurchaseOrders { get; set; }
    public int PendingReceivings { get; set; }
    public int OpenCustomerClaims { get; set; }
    public int OpenSupplierClaims { get; set; }
    public IReadOnlyList<DashboardChartPointViewModel> SalesTrend { get; set; } = Array.Empty<DashboardChartPointViewModel>();
    public IReadOnlyList<DashboardChartPointViewModel> ClaimMix { get; set; } = Array.Empty<DashboardChartPointViewModel>();
    public IReadOnlyList<DashboardActivityViewModel> RecentActivities { get; set; } = Array.Empty<DashboardActivityViewModel>();
    public IReadOnlyList<DashboardAttentionViewModel> AttentionItems { get; set; } = Array.Empty<DashboardAttentionViewModel>();
}
