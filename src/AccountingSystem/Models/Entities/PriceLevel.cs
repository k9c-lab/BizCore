using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PriceLevel
{
    public int PriceLevelId { get; set; }

    [Required]
    [StringLength(30)]
    public string PriceLevelCode { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string PriceLevelName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ItemPrice> ItemPrices { get; set; } = new List<ItemPrice>();
}
