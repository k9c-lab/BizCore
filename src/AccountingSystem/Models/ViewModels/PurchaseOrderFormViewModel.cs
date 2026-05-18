using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class PurchaseOrderFormViewModel
{
    public int? PurchaseOrderId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุเลขที่ใบสั่งซื้อ")]
    [Display(Name = "เลขที่ใบสั่งซื้อ")]
    [StringLength(30)]
    public string PONo { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาระบุวันที่ใบสั่งซื้อ")]
    [DataType(DataType.Date)]
    [Display(Name = "วันที่ใบสั่งซื้อ")]
    public DateTime PODate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาเลือกผู้จำหน่าย")]
    [Display(Name = "ผู้จำหน่าย")]
    public int? SupplierId { get; set; }

    [Display(Name = "สาขา")]
    public int? BranchId { get; set; }

    [Display(Name = "ใบขอซื้อ")]
    public int? PurchaseRequestId { get; set; }

    public string? PurchaseRequestNo { get; set; }
    public List<int> PurchaseRequestIds { get; set; } = new();
    public string PurchaseRequestSourceSummary { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }

    [StringLength(50)]
    [Display(Name = "เลขที่อ้างอิง")]
    public string? ReferenceNo { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "วันที่คาดว่าจะรับสินค้า")]
    public DateTime? ExpectedReceiveDate { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Subtotal { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal DiscountAmount { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกประเภทภาษี")]
    [StringLength(20)]
    [Display(Name = "ประเภทภาษี")]
    public string VatType { get; set; } = "VATExclusive";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "จำนวนภาษีมูลค่าเพิ่ม")]
    public decimal VatAmount { get; set; }

    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "กรุณาระบุสถานะเอกสาร")]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public List<PurchaseOrderLineEditorViewModel> Details { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> SupplierOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> VatTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<PurchaseOrderSupplierLookupViewModel> SupplierLookup { get; set; } = Array.Empty<PurchaseOrderSupplierLookupViewModel>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
}
