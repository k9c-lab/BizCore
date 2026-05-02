IF COL_LENGTH(N'dbo.QuotationHeaders', N'PriceLevelId') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
        ADD PriceLevelId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'PriceLevelId') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD PriceLevelId INT NULL;
END;
GO

IF OBJECT_ID(N'dbo.PriceLevels', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_QuotationHeaders_PriceLevels'
    )
BEGIN
    ALTER TABLE dbo.QuotationHeaders
        ADD CONSTRAINT FK_QuotationHeaders_PriceLevels
            FOREIGN KEY (PriceLevelId) REFERENCES dbo.PriceLevels (PriceLevelId);
END;
GO

IF OBJECT_ID(N'dbo.PriceLevels', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_InvoiceHeaders_PriceLevels'
    )
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD CONSTRAINT FK_InvoiceHeaders_PriceLevels
            FOREIGN KEY (PriceLevelId) REFERENCES dbo.PriceLevels (PriceLevelId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_QuotationHeaders_PriceLevelId'
      AND object_id = OBJECT_ID(N'dbo.QuotationHeaders')
)
BEGIN
    CREATE INDEX IX_QuotationHeaders_PriceLevelId
        ON dbo.QuotationHeaders (PriceLevelId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_InvoiceHeaders_PriceLevelId'
      AND object_id = OBJECT_ID(N'dbo.InvoiceHeaders')
)
BEGIN
    CREATE INDEX IX_InvoiceHeaders_PriceLevelId
        ON dbo.InvoiceHeaders (PriceLevelId);
END;
GO
