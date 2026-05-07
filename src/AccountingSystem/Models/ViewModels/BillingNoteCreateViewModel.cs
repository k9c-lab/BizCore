using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class BillingNoteCreateViewModel
{
    public int? BillingNoteId { get; set; }

    [Required]
    [Display(Name = "เลขที่ใบวางบิล")]
    public string BillingNoteNo { get; set; } = string.Empty;

    [Required]
    [Display(Name = "วันที่ใบวางบิล")]
    [DataType(DataType.Date)]
    public DateTime BillingNoteDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "กรุณาเลือกลูกค้า")]
    [Display(Name = "ลูกค้า")]
    public int? CustomerId { get; set; }

    [Display(Name = "สาขา")]
    public int? BranchId { get; set; }

    [Display(Name = "รูปแบบสรุป")]
    public string SummaryMode { get; set; } = "TreatmentRight";

    [Range(typeof(decimal), "0", "999999999999999.99", ErrorMessage = "ส่วนลดต้องมากกว่าหรือเท่ากับ 0")]
    [Display(Name = "ส่วนลดทั้งใบ")]
    public decimal DiscountAmount { get; set; }

    [StringLength(500)]
    [Display(Name = "หมายเหตุ")]
    public string? Remark { get; set; }

    public string? Search { get; set; }
    public string SubmitAction { get; set; } = "Issue";

    [DataType(DataType.Date)]
    public DateTime? DateFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateTo { get; set; }

    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }
    public bool IsEditMode { get; set; }
    public List<int> SelectedInvoiceIds { get; set; } = new();
    public List<BillingNoteInvoiceCandidateViewModel> AvailableInvoices { get; set; } = new();
    public List<BillingNoteSummaryPreviewViewModel> SummaryPreview { get; set; } = new();
    public decimal SelectedTotalAmount { get; set; }
    public decimal PreviewSubtotalAmount { get; set; }
    public decimal PreviewVatAmount { get; set; }
    public decimal PreviewTotalAmount { get; set; }

    public IEnumerable<SelectListItem> CustomerOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> SummaryModeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
