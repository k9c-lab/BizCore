using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class BillingNoteLine
{
    public int BillingNoteLineId { get; set; }

    [Required]
    public int BillingNoteId { get; set; }

    public int LineNumber { get; set; }

    [Required]
    [StringLength(30)]
    public string SummaryType { get; set; } = "TreatmentRight";

    public int? TreatmentRightId { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public int InvoiceCount { get; set; }

    public decimal TotalAmount { get; set; }

    public BillingNoteHeader? BillingNoteHeader { get; set; }
    public TreatmentRight? TreatmentRight { get; set; }
}
