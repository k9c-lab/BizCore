using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class ReceivingLineEditorViewModel
{
    public int PurchaseOrderDetailId { get; set; }
    public int ItemId { get; set; }
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public bool IsSerialControlled { get; set; }
    public bool TrackStock { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal RemainingQty { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Qty Received")]
    public decimal QtyReceivedInput { get; set; }

    [StringLength(300)]
    public string? Remark { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Supplier Warranty Start Date")]
    public DateTime? SupplierWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Supplier Warranty End Date")]
    public DateTime? SupplierWarrantyEndDate { get; set; }

    [Display(Name = "Serial Numbers")]
    public string? SerialEntryText { get; set; }

    public List<ReceivingSerialEditorViewModel> Serials { get; set; } = new();
}
