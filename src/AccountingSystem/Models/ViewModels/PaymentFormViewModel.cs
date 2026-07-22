using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class PaymentFormViewModel
{
    public int? PaymentId { get; set; }

    [Required]
    [Display(Name = "เลขที่รับชำระ")]
    [StringLength(30)]
    public string PaymentNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วันที่รับชำระ")]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาเลือกลูกค้า")]
    [Display(Name = "ลูกค้า")]
    public int? CustomerId { get; set; }

    [Display(Name = "ใบวางบิล")]
    public int? BillingNoteId { get; set; }

    public string BillingNoteNo { get; set; } = string.Empty;
    public decimal BillingNoteTotalAmount { get; set; }
    public decimal BillingNotePaidAmount { get; set; }
    public decimal BillingNoteBalanceAmount { get; set; }
    public bool LockCustomerSelection { get; set; }

    [Display(Name = "สาขา")]
    public int? BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วิธีการชำระเงิน")]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Transfer";

    [Display(Name = "เลขที่อ้างอิง")]
    [StringLength(100)]
    public string? ReferenceNo { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "จำนวนเงินต้องมากกว่า 0")]
    public decimal Amount { get; set; }

    [Display(Name = "หัก ณ ที่จ่าย 3%")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "ยอดหัก ณ ที่จ่ายต้องไม่ติดลบ")]
    public decimal WhtAmount { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public decimal TotalAppliedAmount { get; set; }
    public decimal UnappliedAmount { get; set; }
    public List<PaymentInvoiceAllocationEditorViewModel> Allocations { get; set; } = new();

    public IEnumerable<SelectListItem> CustomerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> PaymentMethodOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<QuotationCustomerLookupViewModel> CustomerLookup { get; set; } = Array.Empty<QuotationCustomerLookupViewModel>();
}
