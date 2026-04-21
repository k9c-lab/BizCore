using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class CustomerClaimDetail
{
    public int CustomerClaimDetailId { get; set; }
    public int CustomerClaimId { get; set; }
    public int SerialId { get; set; }
    public int ItemId { get; set; }
    public int? OriginalInvoiceId { get; set; }
    public int? ReplacementSerialId { get; set; }

    [StringLength(500)]
    public string? LineRemark { get; set; }

    public CustomerClaimHeader? CustomerClaimHeader { get; set; }
    public SerialNumber? SerialNumber { get; set; }
    public Item? Item { get; set; }
    public InvoiceHeader? OriginalInvoice { get; set; }
    public SerialNumber? ReplacementSerialNumber { get; set; }
}
