using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PurchaseRequestDetail
{
    public int PurchaseRequestDetailId { get; set; }
    public int PurchaseRequestId { get; set; }
    public int LineNumber { get; set; }

    [Required]
    [Display(Name = "Item")]
    public int ItemId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [Display(Name = "Requested Qty")]
    public decimal RequestedQty { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public PurchaseRequestHeader? PurchaseRequestHeader { get; set; }
    public Item? Item { get; set; }
    public ICollection<PurchaseOrderAllocationSource> PurchaseOrderAllocationSources { get; set; } = new List<PurchaseOrderAllocationSource>();
}
