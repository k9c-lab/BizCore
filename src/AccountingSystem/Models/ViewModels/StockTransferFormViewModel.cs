using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class StockTransferFormViewModel
{
    public int? StockTransferId { get; set; }

    [Required]
    [Display(Name = "Transfer No.")]
    [StringLength(30)]
    public string TransferNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Transfer Date")]
    public DateTime TransferDate { get; set; } = DateTime.Today;

    [Display(Name = "From Branch")]
    public int? FromBranchId { get; set; }

    [Display(Name = "To Branch")]
    public int? ToBranchId { get; set; }

    public string FromBranchName { get; set; } = string.Empty;
    public string ToBranchName { get; set; } = string.Empty;
    public bool CanAccessAllBranches { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [StringLength(500)]
    public string? Remark { get; set; }

    public List<StockTransferLineEditorViewModel> Details { get; set; } = new() { new() };

    public IEnumerable<SelectListItem> FromBranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> ToBranchOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<QuotationItemLookupViewModel> ItemLookup { get; set; } = Array.Empty<QuotationItemLookupViewModel>();
}
