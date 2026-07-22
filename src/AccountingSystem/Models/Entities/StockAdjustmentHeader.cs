using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockAdjustmentHeader
{
    public int StockAdjustmentId { get; set; }

    [Required]
    [StringLength(30)]
    public string AdjustmentNo { get; set; } = string.Empty;

    public DateTime AdjustmentDate { get; set; } = DateTime.Today;

    public int BranchId { get; set; }

    [Required]
    [StringLength(30)]
    public string AdjustmentType { get; set; } = "Adjustment";

    [StringLength(500)]
    public string? Remark { get; set; }

    public int? CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Branch? Branch { get; set; }
    public User? CreatedByUser { get; set; }
    public List<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = new();
}
