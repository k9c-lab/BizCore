using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class PurchaseOrderDetail
{
    public int PurchaseOrderDetailId { get; set; }
    public int PurchaseOrderId { get; set; }
    public int LineNumber { get; set; }

    [Required]
    [Display(Name = "Item")]
    public int ItemId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Qty { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Received Qty")]
    public decimal ReceivedQty { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "Line Total")]
    public decimal LineTotal { get; set; }

    [StringLength(300)]
    public string? Remark { get; set; }

    public PurchaseOrderHeader? PurchaseOrderHeader { get; set; }
    public Item? Item { get; set; }
    public ICollection<ReceivingDetail> ReceivingDetails { get; set; } = new List<ReceivingDetail>();
}
