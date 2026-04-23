using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class InvoiceFormViewModel
{
    public int? InvoiceId { get; set; }
    public int? QuotationId { get; set; }

    [Required]
    [Display(Name = "Invoice No.")]
    [StringLength(30)]
    public string InvoiceNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Invoice Date")]
    [DataType(DataType.Date)]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Please select a customer.")]
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Salesperson")]
    public int? SalespersonId { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }

    [Display(Name = "Quotation No.")]
    public string? QuotationNo { get; set; }

    [Display(Name = "Reference No.")]
    [StringLength(50)]
    public string? ReferenceNo { get; set; }

    [Required]
    [Display(Name = "VAT Type")]
    [StringLength(10)]
    public string VatType { get; set; } = "NoVAT";

    [Required]
    [Display(Name = "Discount Mode")]
    [StringLength(10)]
    public string DiscountMode { get; set; } = "Line";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Header Discount")]
    public decimal HeaderDiscountAmount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = "Issued";

    public List<InvoiceLineEditorViewModel> Details { get; set; } = new();

    public IEnumerable<SelectListItem> CustomerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SalespersonOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> DiscountModeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> VatTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<InvoiceItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<InvoiceItemLookupViewModel>();
    public IReadOnlyList<InvoiceSerialLookupViewModel> SerialLookup { get; set; } = Array.Empty<InvoiceSerialLookupViewModel>();
    public IReadOnlyList<QuotationCustomerLookupViewModel> CustomerLookup { get; set; } = Array.Empty<QuotationCustomerLookupViewModel>();
    public IReadOnlyList<QuotationSalespersonLookupViewModel> SalespersonLookup { get; set; } = Array.Empty<QuotationSalespersonLookupViewModel>();
}
