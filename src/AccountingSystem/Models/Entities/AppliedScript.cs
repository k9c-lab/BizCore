using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class AppliedScript
{
    public int AppliedScriptId { get; set; }

    [Required]
    [StringLength(200)]
    public string ScriptName { get; set; } = string.Empty;

    [StringLength(64)]
    public string? ScriptHash { get; set; }

    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;

    public int? AppliedByUserId { get; set; }

    public User? AppliedByUser { get; set; }
}
