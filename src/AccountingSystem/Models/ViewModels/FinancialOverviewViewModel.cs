using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class FinancialOverviewViewModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public IReadOnlyList<SelectListItem> BranchOptions { get; set; } = Array.Empty<SelectListItem>();

    public decimal RevenueTotal { get; set; }
    public decimal CollectedAr { get; set; }
    public decimal OutstandingAr { get; set; }
    public decimal PurchaseBase { get; set; }
    public decimal PaidAp { get; set; }
    public decimal OutstandingAp { get; set; }

    public decimal ArCollectedPercent => RevenueTotal <= 0m ? 0m : Math.Round(CollectedAr / RevenueTotal * 100m, 2);
    public decimal ArOutstandingPercent => RevenueTotal <= 0m ? 0m : Math.Round(OutstandingAr / RevenueTotal * 100m, 2);
    public decimal ApPaidPercent => PurchaseBase <= 0m ? 0m : Math.Round(PaidAp / PurchaseBase * 100m, 2);
    public decimal ApOutstandingPercent => PurchaseBase <= 0m ? 0m : Math.Round(OutstandingAp / PurchaseBase * 100m, 2);

    public IReadOnlyList<ReceivableDocumentRowViewModel> ReceivableRows { get; set; } = Array.Empty<ReceivableDocumentRowViewModel>();
    public IReadOnlyList<PartyBalanceRowViewModel> ReceivableCustomerRows { get; set; } = Array.Empty<PartyBalanceRowViewModel>();
    public IReadOnlyList<PayableDocumentRowViewModel> PayableRows { get; set; } = Array.Empty<PayableDocumentRowViewModel>();
    public IReadOnlyList<PartyBalanceRowViewModel> PayableSupplierRows { get; set; } = Array.Empty<PartyBalanceRowViewModel>();
}
