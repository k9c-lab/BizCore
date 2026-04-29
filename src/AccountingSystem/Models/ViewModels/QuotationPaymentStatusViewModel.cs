namespace BizCore.Models.ViewModels;

public class QuotationPaymentStatusViewModel
{
    public int QuotationHeaderId { get; set; }
    public int InvoiceCount { get; set; }
    public decimal QuotationTotalAmount { get; set; }
    public decimal InvoicedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "Unpaid";
}
