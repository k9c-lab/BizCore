using BizCore.Data;
using BizCore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BizCore.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly AccountingDbContext _context;
    private const string EnsureSystemSettingsSql = """
        IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.SystemSettings
            (
                SystemSettingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
                SettingKey NVARCHAR(100) NOT NULL,
                SettingValue NVARCHAR(200) NOT NULL,
                Description NVARCHAR(500) NULL,
                UpdatedByUserId INT NULL,
                UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT UX_SystemSettings_SettingKey UNIQUE (SettingKey),
                CONSTRAINT FK_SystemSettings_Users_UpdatedByUserId FOREIGN KEY (UpdatedByUserId)
                    REFERENCES dbo.Users (UserId)
            );
        END;
        """;

    public SystemSettingService(AccountingDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetPricingModeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _context.SystemSettings
                .AsNoTracking()
                .Where(x => x.SettingKey == SettingKeys.SalesPricingMode)
                .Select(x => x.SettingValue)
                .FirstOrDefaultAsync(cancellationToken);

            return PricingModes.All.Contains(value, StringComparer.OrdinalIgnoreCase)
                ? value!
                : PricingModes.SinglePrice;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is Microsoft.Data.SqlClient.SqlException || ex is InvalidOperationException)
        {
            return PricingModes.SinglePrice;
        }
    }

    public async Task SetPricingModeAsync(string pricingMode, int? updatedByUserId, CancellationToken cancellationToken = default)
    {
        if (!PricingModes.All.Contains(pricingMode, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(pricingMode), pricingMode, "Unsupported pricing mode.");
        }

        await _context.Database.ExecuteSqlRawAsync(EnsureSystemSettingsSql, cancellationToken);

        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(x => x.SettingKey == SettingKeys.SalesPricingMode, cancellationToken);

        if (setting is null)
        {
            setting = new SystemSetting
            {
                SettingKey = SettingKeys.SalesPricingMode,
                Description = "Controls whether sales pricing uses the legacy single-price flow or future multi-price flow."
            };
            _context.SystemSettings.Add(setting);
        }

        setting.SettingValue = pricingMode;
        setting.UpdatedByUserId = updatedByUserId;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> GetEnablePatientInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _context.SystemSettings
                .AsNoTracking()
                .Where(x => x.SettingKey == SettingKeys.SalesEnablePatientInfo)
                .Select(x => x.SettingValue)
                .FirstOrDefaultAsync(cancellationToken);

            return value is null || !bool.TryParse(value, out var enabled)
                ? true
                : enabled;
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is Microsoft.Data.SqlClient.SqlException || ex is InvalidOperationException)
        {
            return true;
        }
    }

    public async Task SetEnablePatientInfoAsync(bool enabled, int? updatedByUserId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlRawAsync(EnsureSystemSettingsSql, cancellationToken);

        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(x => x.SettingKey == SettingKeys.SalesEnablePatientInfo, cancellationToken);

        if (setting is null)
        {
            setting = new SystemSetting
            {
                SettingKey = SettingKeys.SalesEnablePatientInfo,
                Description = "Controls whether invoice screens and prints show patient information fields."
            };
            _context.SystemSettings.Add(setting);
        }

        setting.SettingValue = enabled.ToString().ToLowerInvariant();
        setting.UpdatedByUserId = updatedByUserId;
        setting.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
