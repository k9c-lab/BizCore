namespace BizCore.Services;

public interface ISystemSettingService
{
    Task<string> GetPricingModeAsync(CancellationToken cancellationToken = default);
    Task SetPricingModeAsync(string pricingMode, int? updatedByUserId, CancellationToken cancellationToken = default);
    Task<bool> GetEnablePatientInfoAsync(CancellationToken cancellationToken = default);
    Task SetEnablePatientInfoAsync(bool enabled, int? updatedByUserId, CancellationToken cancellationToken = default);
    Task<(string Name, string Title)> GetAuthorisedSignatureAsync(CancellationToken cancellationToken = default);
    Task SetAuthorisedSignatureAsync(string name, string title, int? updatedByUserId, CancellationToken cancellationToken = default);
}
