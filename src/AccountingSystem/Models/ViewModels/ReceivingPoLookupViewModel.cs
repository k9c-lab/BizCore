namespace BizCore.Models.ViewModels;

public class ReceivingPoLookupViewModel
{
    public int PurchaseOrderId { get; set; }
    public string PONo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string VatType { get; set; } = "VAT";
    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ReceivingPoLookupLineViewModel> Lines { get; set; } = new();
}
