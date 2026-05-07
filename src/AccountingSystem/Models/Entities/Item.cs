using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Item : IValidatableObject
{
    public int ItemId { get; set; }

    [Required]
    [Display(Name = "รหัสสินค้า")]
    [StringLength(30)]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ชื่อสินค้า")]
    [StringLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [Display(Name = "Part Number")]
    [Required]
    [StringLength(80)]
    public string PartNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "ประเภท")]
    [StringLength(30)]
    public string ItemType { get; set; } = "Product";

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = "EA";

    [Display(Name = "ตัดสต็อก")]
    public bool TrackStock { get; set; } = true;

    [Display(Name = "ควบคุม Serial")]
    public bool IsSerialControlled { get; set; }

    [Range(0, 9999999999999999.99)]
    [Display(Name = "ราคาต่อหน่วย")]
    public decimal UnitPrice { get; set; }

    [Range(0, 9999999999999999.99)]
    [Display(Name = "สต็อกปัจจุบัน")]
    public decimal CurrentStock { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<SerialNumber> SerialNumbers { get; set; } = new List<SerialNumber>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsSerialControlled && !TrackStock)
        {
            yield return new ValidationResult(
                "Serial-controlled items must also track stock.",
                new[] { nameof(IsSerialControlled), nameof(TrackStock) });
        }

        if (!TrackStock && CurrentStock > 0)
        {
            yield return new ValidationResult(
                "Non-stock items should not carry current stock.",
                new[] { nameof(CurrentStock), nameof(TrackStock) });
        }
    }
}
