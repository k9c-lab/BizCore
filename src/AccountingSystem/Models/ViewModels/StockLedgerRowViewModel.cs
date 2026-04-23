namespace BizCore.Models.ViewModels;

public class StockLedgerRowViewModel
{
    public int StockMovementId { get; set; }
    public DateTime MovementDate { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public int? SerialId { get; set; }
    public string SerialNo { get; set; } = "-";
    public int? FromBranchId { get; set; }
    public string FromBranchName { get; set; } = "-";
    public int? ToBranchId { get; set; }
    public string ToBranchName { get; set; } = "-";
    public decimal Qty { get; set; }
    public decimal InQty { get; set; }
    public decimal OutQty { get; set; }
    public string? Remark { get; set; }
    public string CreatedByName { get; set; } = "-";

    public string ReferenceText => string.IsNullOrWhiteSpace(ReferenceType)
        ? "-"
        : ReferenceId.HasValue ? $"{ReferenceType} #{ReferenceId}" : ReferenceType;
}
