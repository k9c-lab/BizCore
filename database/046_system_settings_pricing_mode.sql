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
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE SettingKey = N'Sales.PricingMode')
BEGIN
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description)
    VALUES
    (
        N'Sales.PricingMode',
        N'SinglePrice',
        N'Controls whether sales pricing uses the legacy single-price flow or the future multi-price flow.'
    );
END;
GO
