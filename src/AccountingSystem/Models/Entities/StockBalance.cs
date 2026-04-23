using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockBalance
{
    public int StockBalanceId { get; set; }

    public int ItemId { get; set; }

    public int BranchId { get; set; }

    [Range(0, 9999999999999999.99)]
    public decimal QtyOnHand { get; set; }

    public Item? Item { get; set; }
    public Branch? Branch { get; set; }
}
