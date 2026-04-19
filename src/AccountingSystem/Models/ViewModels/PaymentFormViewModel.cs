using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class PaymentFormViewModel
{
    public int? PaymentId { get; set; }

    [Required]
    [Display(Name = "Payment No.")]
    [StringLength(30)]
    public string PaymentNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Please select a customer.")]
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

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

    public decimal TotalAppliedAmount { get; set; }
    public decimal UnappliedAmount { get; set; }
    public List<PaymentInvoiceAllocationEditorViewModel> Allocations { get; set; } = new();

    public IEnumerable<SelectListItem> CustomerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PaymentMethodOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
