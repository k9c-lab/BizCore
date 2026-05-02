using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class SystemSetting
{
    public int SystemSettingId { get; set; }

    [Required]
    [StringLength(100)]
    public string SettingKey { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string SettingValue { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? UpdatedByUser { get; set; }
}
