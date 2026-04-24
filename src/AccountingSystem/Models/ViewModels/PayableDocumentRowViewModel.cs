namespace BizCore.Models.ViewModels;

public class PayableDocumentRowViewModel
{
    public int PurchaseOrderId { get; set; }
    public string PONo { get; set; } = string.Empty;
    public DateTime PODate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
