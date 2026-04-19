using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PaymentAllocation
{
    public int PaymentAllocationId { get; set; }
    public int PaymentId { get; set; }
    public int InvoiceId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [Display(Name = "Applied Amount")]
    public decimal AppliedAmount { get; set; }

    public PaymentHeader? PaymentHeader { get; set; }
    public InvoiceHeader? InvoiceHeader { get; set; }
}
