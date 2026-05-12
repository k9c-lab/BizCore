using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class QuotationLineEditorViewModel
{
    public int? QuotationDetailId { get; set; }

    public int LineNumber { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกรายการสินค้า")]
    [Display(Name = "สินค้า")]
    public int? ItemId { get; set; }

    [StringLength(1000, ErrorMessage = "รายละเอียดต้องมีความยาวไม่เกิน 1000 ตัวอักษร")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "จำนวนต้องมากกว่า 0")]
    public decimal Quantity { get; set; } = 1m;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "ราคาต่อหน่วย")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "ส่วนลด")]
    public decimal DiscountAmount { get; set; }

    [Display(Name = "ประเภทส่วนลด")]
    [StringLength(10)]
    public string DiscountType { get; set; } = "Amount";

    [Range(typeof(decimal), "0", "100", ErrorMessage = "เปอร์เซ็นต์ส่วนลดต้องอยู่ระหว่าง 0 ถึง 100")]
    [Display(Name = "ส่วนลด %")]
    public decimal DiscountPercent { get; set; }

    [Display(Name = "รวมบรรทัด")]
    public decimal LineTotal { get; set; }

    public decimal NetUnitPrice => Quantity > 0 ? Math.Round(LineTotal / Quantity, 2, MidpointRounding.AwayFromZero) : 0m;
}
