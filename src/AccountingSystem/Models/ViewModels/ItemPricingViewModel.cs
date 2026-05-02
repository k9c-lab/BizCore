namespace BizCore.Models.ViewModels;

public class ItemPricingViewModel
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public decimal BaseUnitPrice { get; set; }
    public string PricingMode { get; set; } = string.Empty;
    public List<ItemPriceEditorRowViewModel> PriceRows { get; set; } = new();
}
