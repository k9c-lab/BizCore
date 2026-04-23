using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.ViewModels;

public class PurchaseOrderAllocationEditorViewModel
{
    public int? PurchaseOrderAllocationId { get; set; }
    public int? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal AllocatedQty { get; set; }

    public decimal ReceivedQty { get; set; }
    public List<PurchaseOrderAllocationSourceEditorViewModel> Sources { get; set; } = new();
}
