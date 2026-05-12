using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class CashSaleDetail
{
    public int CashSaleDetailId { get; set; }
    public int CashSaleId { get; set; }
    public int LineNumber { get; set; }

    [Required]
    [Display(Name = "Item")]
    public int ItemId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Qty { get; set; } = 1m;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Discount")]
    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }

    [StringLength(2000)]
    public string? Remark { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty Start Date")]
    public DateTime? CustomerWarrantyStartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Customer Warranty End Date")]
    public DateTime? CustomerWarrantyEndDate { get; set; }

    public CashSaleHeader? CashSaleHeader { get; set; }
    public Item? Item { get; set; }
    public ICollection<CashSaleSerial> CashSaleSerials { get; set; } = new List<CashSaleSerial>();
}
