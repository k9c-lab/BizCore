namespace BizCore.Models.ViewModels;

public class ReceivingPoLookupLineViewModel
{
    public int PurchaseOrderDetailId { get; set; }
    public int ItemId { get; set; }
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public bool IsSerialControlled { get; set; }
    public bool TrackStock { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal RemainingQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
