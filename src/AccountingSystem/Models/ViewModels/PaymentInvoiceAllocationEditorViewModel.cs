using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class PaymentInvoiceAllocationEditorViewModel
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Applied Amount")]
    public decimal AppliedAmount { get; set; }
}
