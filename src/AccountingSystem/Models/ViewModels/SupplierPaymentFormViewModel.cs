using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class SupplierPaymentFormViewModel
{
    public int? SupplierPaymentId { get; set; }

    [Required]
    [Display(Name = "เลขที่เอกสารชำระเงิน")]
    [StringLength(30)]
    public string PaymentNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วันที่ชำระเงิน")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาเลือกใบสั่งซื้อ")]
    [Display(Name = "ใบสั่งซื้อ")]
    public int? PurchaseOrderId { get; set; }

    public int? SupplierId { get; set; }
    public int? BranchId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string PurchaseOrderNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วิธีการชำระเงิน")]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Transfer";

    [Display(Name = "เลขที่อ้างอิง")]
    [StringLength(100)]
    public string? ReferenceNo { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "จำนวนเงินต้องมากกว่า 0")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public decimal PurchaseOrderTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    public IEnumerable<SelectListItem> PurchaseOrderOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PaymentMethodOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<SupplierPaymentPurchaseOrderLookupViewModel> PurchaseOrderLookup { get; set; } = Array.Empty<SupplierPaymentPurchaseOrderLookupViewModel>();
}
