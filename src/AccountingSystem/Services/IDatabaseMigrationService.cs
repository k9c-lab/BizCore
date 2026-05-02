using BizCore.Models.ViewModels;

namespace BizCore.Services;

public interface IDatabaseMigrationService
{
    Task<DatabaseMigrationStatusViewModel> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<int> RunPendingAsync(int? appliedByUserId, CancellationToken cancellationToken = default);
}
