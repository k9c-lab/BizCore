namespace BizCore.Models.ViewModels;

public class PurchaseOrderAllocationSourceEditorViewModel
{
    public int? PurchaseOrderAllocationSourceId { get; set; }
    public int? PurchaseRequestDetailId { get; set; }
    public int? PurchaseRequestId { get; set; }
    public string PurchaseRequestNo { get; set; } = string.Empty;
    public decimal SourceQty { get; set; }
}
