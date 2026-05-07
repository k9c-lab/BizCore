using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class BillingNoteInvoice
{
    public int BillingNoteInvoiceId { get; set; }

    [Required]
    public int BillingNoteId { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    [Display(Name = "ยอดที่รวมในใบวางบิล")]
    public decimal BilledAmount { get; set; }

    public BillingNoteHeader? BillingNoteHeader { get; set; }
    public InvoiceHeader? InvoiceHeader { get; set; }
}
