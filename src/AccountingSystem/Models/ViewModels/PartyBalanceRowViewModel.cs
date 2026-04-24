namespace BizCore.Models.ViewModels;

public class PartyBalanceRowViewModel
{
    public string PartyName { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
}
