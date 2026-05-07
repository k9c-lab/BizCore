namespace BizCore.Models.ViewModels;

public class BillingNoteSummaryPreviewViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalAmount { get; set; }
}
