using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ReceivingDetail
{
    public int ReceivingDetailId { get; set; }
    public int ReceivingId { get; set; }
    public int PurchaseOrderDetailId { get; set; }
    public int ItemId { get; set; }
    public int LineNumber { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    [Display(Name = "Qty Received")]
    public decimal QtyReceived { get; set; }

    [StringLength(300)]
    public string? Remark { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Supplier Warranty Start Date")]
    public DateTime? SupplierWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Supplier Warranty End Date")]
    public DateTime? SupplierWarrantyEndDate { get; set; }

    public ReceivingHeader? ReceivingHeader { get; set; }
    public PurchaseOrderDetail? PurchaseOrderDetail { get; set; }
    public Item? Item { get; set; }
    public ICollection<ReceivingSerial> ReceivingSerials { get; set; } = new List<ReceivingSerial>();
}
