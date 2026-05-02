namespace BizCore.Models.ViewModels;

public class DatabaseMigrationStatusViewModel
{
    public IReadOnlyList<DatabaseMigrationScriptViewModel> PendingScripts { get; set; } = Array.Empty<DatabaseMigrationScriptViewModel>();
    public IReadOnlyList<DatabaseMigrationScriptViewModel> AppliedScripts { get; set; } = Array.Empty<DatabaseMigrationScriptViewModel>();
    public bool CanRunMigrations => PendingScripts.Count > 0;
}
