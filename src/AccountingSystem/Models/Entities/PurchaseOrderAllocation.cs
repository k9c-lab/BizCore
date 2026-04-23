using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PurchaseOrderAllocation
{
    public int PurchaseOrderAllocationId { get; set; }
    public int PurchaseOrderDetailId { get; set; }

    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [Display(Name = "Allocated Qty")]
    public decimal AllocatedQty { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Received Qty")]
    public decimal ReceivedQty { get; set; }

    public PurchaseOrderDetail? PurchaseOrderDetail { get; set; }
    public Branch? Branch { get; set; }
    public ICollection<PurchaseOrderAllocationSource> PurchaseOrderAllocationSources { get; set; } = new List<PurchaseOrderAllocationSource>();
    public ICollection<ReceivingDetail> ReceivingDetails { get; set; } = new List<ReceivingDetail>();
}
