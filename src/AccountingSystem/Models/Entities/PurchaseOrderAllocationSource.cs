using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PurchaseOrderAllocationSource
{
    public int PurchaseOrderAllocationSourceId { get; set; }
    public int PurchaseOrderAllocationId { get; set; }
    public int PurchaseRequestDetailId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal SourceQty { get; set; }

    public PurchaseOrderAllocation? PurchaseOrderAllocation { get; set; }
    public PurchaseRequestDetail? PurchaseRequestDetail { get; set; }
}
