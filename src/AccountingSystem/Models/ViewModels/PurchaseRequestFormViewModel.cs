using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class PurchaseRequestFormViewModel
{
    public int? PurchaseRequestId { get; set; }

    [Required]
    [Display(Name = "PR No.")]
    [StringLength(30)]
    public string PRNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Request Date")]
    public DateTime RequestDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Required Date")]
    public DateTime? RequiredDate { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }

    [StringLength(1000)]
    public string? Purpose { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public List<PurchaseRequestLineEditorViewModel> Details { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
}
