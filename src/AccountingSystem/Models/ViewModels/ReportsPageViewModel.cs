using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class ReportsPageViewModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int? BranchId { get; set; }
    public int? SalespersonId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> SalespersonOptions { get; set; } = Array.Empty<SelectListItem>();
    public decimal SalesTotal { get; set; }
    public decimal PaymentsTotal { get; set; }
    public decimal SupplierPaymentsTotal { get; set; }
    public decimal OutstandingAr { get; set; }
    public decimal OutstandingAp { get; set; }
    public decimal CollectedAr { get; set; }
    public decimal PaidAp { get; set; }
    public decimal StockQty { get; set; }
    public IReadOnlyList<SalesReportRowViewModel> SalesRows { get; set; } = Array.Empty<SalesReportRowViewModel>();
    public IReadOnlyList<ReceivableDocumentRowViewModel> ReceivableRows { get; set; } = Array.Empty<ReceivableDocumentRowViewModel>();
    public IReadOnlyList<PartyBalanceRowViewModel> ReceivableCustomerRows { get; set; } = Array.Empty<PartyBalanceRowViewModel>();
    public IReadOnlyList<PayableDocumentRowViewModel> PayableRows { get; set; } = Array.Empty<PayableDocumentRowViewModel>();
    public IReadOnlyList<PartyBalanceRowViewModel> PayableSupplierRows { get; set; } = Array.Empty<PartyBalanceRowViewModel>();
    public IReadOnlyList<StockReportRowViewModel> StockRows { get; set; } = Array.Empty<StockReportRowViewModel>();
    public IReadOnlyList<MovementReportRowViewModel> MovementRows { get; set; } = Array.Empty<MovementReportRowViewModel>();
    public IReadOnlyList<ClaimReportRowViewModel> ClaimRows { get; set; } = Array.Empty<ClaimReportRowViewModel>();
}
