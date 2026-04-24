using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class SupplierPaymentFormViewModel
{
    public int? SupplierPaymentId { get; set; }

    [Required]
    [Display(Name = "Payment No.")]
    [StringLength(30)]
    public string PaymentNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Please select a purchase order.")]
    [Display(Name = "Purchase Order")]
    public int? PurchaseOrderId { get; set; }

    public int? SupplierId { get; set; }
    public int? BranchId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string PurchaseOrderNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Payment Method")]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Transfer";

    [Display(Name = "Reference No.")]
    [StringLength(100)]
    public string? ReferenceNo { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Amount must be greater than zero.")]
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
