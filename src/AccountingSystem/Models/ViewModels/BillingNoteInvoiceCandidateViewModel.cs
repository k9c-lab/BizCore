namespace BizCore.Models.ViewModels;

public class BillingNoteInvoiceCandidateViewModel
{
    public int InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientHn { get; set; } = string.Empty;
    public string TreatmentRightName { get; set; } = string.Empty;
    public decimal BalanceAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public string VatType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public List<BillingNoteInvoiceItemCandidateViewModel> ItemLines { get; set; } = new();
}
