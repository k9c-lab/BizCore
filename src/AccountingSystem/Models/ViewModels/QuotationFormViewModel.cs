using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class QuotationFormViewModel
{
    public int? QuotationHeaderId { get; set; }

    [Required]
    [Display(Name = "Quotation No.")]
    [StringLength(30)]
    public string QuotationNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Quotation Date")]
    [DataType(DataType.Date)]
    public DateTime QuotationDate { get; set; } = DateTime.Today;

    [Display(Name = "Expiry Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [Required(ErrorMessage = "Please select a customer.")]
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Salesperson")]
    public int? SalespersonId { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }

    [Display(Name = "Reference No.")]
    [StringLength(50)]
    public string? ReferenceNo { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [Required]
    [Display(Name = "VAT Type")]
    [StringLength(10)]
    public string VatType { get; set; } = "NoVAT";

    [StringLength(500)]
    public string? Remarks { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }

    [Required]
    [Display(Name = "Discount Mode")]
    [StringLength(10)]
    public string DiscountMode { get; set; } = "Line";

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal HeaderDiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public List<QuotationLineEditorViewModel> Details { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> CustomerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SalespersonOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> DiscountModeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> VatTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
    public IReadOnlyList<QuotationCustomerLookupViewModel> CustomerLookup { get; set; } = Array.Empty<QuotationCustomerLookupViewModel>();
    public IReadOnlyList<QuotationSalespersonLookupViewModel> SalespersonLookup { get; set; } = Array.Empty<QuotationSalespersonLookupViewModel>();
}
