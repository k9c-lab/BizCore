using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class StockMovement
{
    public int StockMovementId { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.Today;

    [Required]
    [StringLength(30)]
    public string MovementType { get; set; } = string.Empty;

    [StringLength(30)]
    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public int ItemId { get; set; }

    public int? SerialId { get; set; }

    public int? FromBranchId { get; set; }

    public int? ToBranchId { get; set; }

    public decimal Qty { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Item? Item { get; set; }
    public SerialNumber? SerialNumber { get; set; }
    public Branch? FromBranch { get; set; }
    public Branch? ToBranch { get; set; }
    public User? CreatedByUser { get; set; }
}
