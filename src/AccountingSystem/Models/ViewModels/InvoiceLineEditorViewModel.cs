using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class InvoiceLineEditorViewModel
{
    public int? InvoiceDetailId { get; set; }
    public int LineNumber { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกรายการสินค้า")]
    [Display(Name = "สินค้า")]
    public int? ItemId { get; set; }

    public int? QuotationDetailId { get; set; }

    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Product";
    public bool TrackStock { get; set; }
    public bool IsSerialControlled { get; set; }
    public decimal CurrentStock { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "จำนวนอ้างอิงต้องมากกว่า 0")]
    public decimal QuotedQty { get; set; } = 1m;

    public decimal? PreviouslyInvoicedQty { get; set; }
    public decimal? RemainingQuotedQty { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "จำนวนต้องมากกว่า 0")]
    public decimal Qty { get; set; } = 1m;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "ราคาต่อหน่วย")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "ส่วนลด")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "รวมบรรทัด")]
    public decimal LineTotal { get; set; }

    [StringLength(2000, ErrorMessage = "หมายเหตุรายการต้องมีความยาวไม่เกิน 2000 ตัวอักษร")]
    public string? Remark { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "วันที่เริ่มประกันลูกค้า")]
    public DateTime? CustomerWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "วันที่สิ้นสุดประกันลูกค้า")]
    public DateTime? CustomerWarrantyEndDate { get; set; }

    public List<int> SelectedSerialIds { get; set; } = new();
}
