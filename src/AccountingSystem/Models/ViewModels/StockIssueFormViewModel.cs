using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class StockIssueFormViewModel
{
    public int? StockIssueId { get; set; }

    [Required]
    [Display(Name = "Issue No.")]
    [StringLength(30)]
    public string IssueNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Issue Date")]
    public DateTime IssueDate { get; set; } = DateTime.Today;

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }

    [Required]
    [Display(Name = "Issue Type")]
    [StringLength(30)]
    public string IssueType { get; set; } = "InternalUse";

    [StringLength(500)]
    public string? Purpose { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(500)]
    public string? Remark { get; set; }

    public List<StockIssueLineEditorViewModel> Details { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> IssueTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
}
