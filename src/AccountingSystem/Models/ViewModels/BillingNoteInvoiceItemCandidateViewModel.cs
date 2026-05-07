namespace BizCore.Models.ViewModels;

public class BillingNoteInvoiceItemCandidateViewModel
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal LineTotal { get; set; }
}
