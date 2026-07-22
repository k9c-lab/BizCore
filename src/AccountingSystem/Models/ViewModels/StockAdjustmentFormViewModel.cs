using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class StockAdjustmentFormViewModel
{
    [Required]
    [Display(Name = "เลขที่เอกสาร")]
    [StringLength(30)]
    public string AdjustmentNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "วันที่")]
    public DateTime AdjustmentDate { get; set; } = DateTime.Today;

    [Display(Name = "สาขา")]
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }

    [Required]
    [Display(Name = "ประเภทการปรับ")]
    [StringLength(30)]
    public string AdjustmentType { get; set; } = "Adjustment";

    [StringLength(500)]
    [Display(Name = "หมายเหตุ")]
    public string? Remark { get; set; }

    public List<StockAdjustmentLineEditorViewModel> Lines { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> BranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> AdjustmentTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
}
