namespace BizCore.Services;

public interface ISystemSettingService
{
    Task<string> GetPricingModeAsync(CancellationToken cancellationToken = default);
    Task SetPricingModeAsync(string pricingMode, int? updatedByUserId, CancellationToken cancellationToken = default);
}
