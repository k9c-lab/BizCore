using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class ReceivingSerial
{
    public int ReceivingSerialId { get; set; }
    public int ReceivingDetailId { get; set; }
    public int ItemId { get; set; }

    [Required]
    [StringLength(120)]
    [Display(Name = "Serial No.")]
    public string SerialNo { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ReceivingDetail? ReceivingDetail { get; set; }
    public Item? Item { get; set; }
}
