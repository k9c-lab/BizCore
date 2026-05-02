namespace BizCore.Models.ViewModels;

public class DatabaseMigrationScriptViewModel
{
    public string ScriptName { get; set; } = string.Empty;
    public DateTime? AppliedAtUtc { get; set; }
    public string? ScriptHash { get; set; }
}
