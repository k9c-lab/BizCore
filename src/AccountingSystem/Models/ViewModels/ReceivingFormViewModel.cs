using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BizCore.Models.ViewModels;

public class ReceivingFormViewModel
{
    [Required]
    [Display(Name = "Receiving No.")]
    [StringLength(30)]
    public string ReceivingNo { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Receive Date")]
    public DateTime ReceiveDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Purchase Order")]
    public int? PurchaseOrderId { get; set; }

    [Required]
    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [StringLength(50)]
    [Display(Name = "Delivery Note No.")]
    public string? DeliveryNoteNo { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public string Status { get; set; } = "Posted";

    public List<ReceivingLineEditorViewModel> Details { get; set; } = new();

    public IEnumerable<SelectListItem> PurchaseOrderOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<ReceivingPoLookupViewModel> PurchaseOrderLookup { get; set; } = Array.Empty<ReceivingPoLookupViewModel>();
}
