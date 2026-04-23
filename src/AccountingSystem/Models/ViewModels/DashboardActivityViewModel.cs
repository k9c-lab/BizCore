namespace BizCore.Models.ViewModels;

public class DashboardActivityViewModel
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string Controller { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
}
