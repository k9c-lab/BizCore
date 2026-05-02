using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class ItemPriceEditorRowViewModel
{
    public int? ItemPriceId { get; set; }

    public int PriceLevelId { get; set; }

    public string PriceLevelCode { get; set; } = string.Empty;

    public string PriceLevelName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 9999999999999999.99)]
    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;
}
