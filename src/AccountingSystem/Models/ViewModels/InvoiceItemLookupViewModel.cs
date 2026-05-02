namespace BizCore.Models.ViewModels;

public class InvoiceItemLookupViewModel
{
    public int ItemId { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Product";
    public decimal UnitPrice { get; set; }
    public Dictionary<int, decimal> PriceLevelPrices { get; set; } = new();
    public bool TrackStock { get; set; }
    public bool IsSerialControlled { get; set; }
    public decimal CurrentStock { get; set; }
}
