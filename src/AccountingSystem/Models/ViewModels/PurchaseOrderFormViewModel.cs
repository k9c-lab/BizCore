using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class PurchaseOrderFormViewModel
{
    public int? PurchaseOrderId { get; set; }

    [Required]
    [Display(Name = "PO No.")]
    [StringLength(30)]
    public string PONo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "PO Date")]
    public DateTime PODate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [StringLength(50)]
    [Display(Name = "Reference No.")]
    public string? ReferenceNo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Expected Receive Date")]
    public DateTime? ExpectedReceiveDate { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Subtotal { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "VAT Type")]
    public string VatType { get; set; } = "VAT";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "VAT Amount")]
    public decimal VatAmount { get; set; }

    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public List<PurchaseOrderLineEditorViewModel> Details { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> VatTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<PurchaseOrderSupplierLookupViewModel> SupplierLookup { get; set; } = Array.Empty<PurchaseOrderSupplierLookupViewModel>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
}
