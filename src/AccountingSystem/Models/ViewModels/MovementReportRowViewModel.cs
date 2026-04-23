namespace BizCore.Models.ViewModels;

public class MovementReportRowViewModel
{
    public string MovementType { get; set; } = string.Empty;
    public int MovementCount { get; set; }
    public decimal Quantity { get; set; }
}
