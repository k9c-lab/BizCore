using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ReceiptPrintLine
{
    public int ReceiptPrintLineId { get; set; }

    [Required]
    public int ReceiptId { get; set; }

    public int LineNumber { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public ReceiptHeader? ReceiptHeader { get; set; }
}
