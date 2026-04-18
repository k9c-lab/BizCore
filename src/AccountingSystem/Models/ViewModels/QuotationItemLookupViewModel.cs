namespace BizCore.Models.ViewModels;

public class QuotationItemLookupViewModel
{
    public int ItemId { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsSerialControlled { get; set; }
}
