namespace BizCore.Models.ViewModels;

public class SalesReportRowViewModel
{
    public DateTime Date { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
}
