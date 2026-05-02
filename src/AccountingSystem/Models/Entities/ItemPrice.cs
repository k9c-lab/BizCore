using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ItemPrice
{
    public int ItemPriceId { get; set; }

    public int ItemId { get; set; }

    public int PriceLevelId { get; set; }

    [Range(0, 9999999999999999.99)]
    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public Item? Item { get; set; }

    public PriceLevel? PriceLevel { get; set; }
}
