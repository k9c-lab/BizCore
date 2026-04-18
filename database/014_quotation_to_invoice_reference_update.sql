IF COL_LENGTH('dbo.InvoiceHeaders', 'QuotationId') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
    ADD QuotationId INT NULL;
END;
GO

IF COL_LENGTH('dbo.InvoiceHeaders', 'ReferenceNo') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
    ADD ReferenceNo NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'ReferenceNo') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD ReferenceNo NVARCHAR(50) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_InvoiceHeaders_QuotationHeaders'
)
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
    ADD CONSTRAINT FK_InvoiceHeaders_QuotationHeaders
        FOREIGN KEY (QuotationId) REFERENCES dbo.QuotationHeaders (QuotationHeaderId);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_InvoiceHeaders_QuotationId'
      AND object_id = OBJECT_ID(N'dbo.InvoiceHeaders')
)
BEGIN
    CREATE INDEX IX_InvoiceHeaders_QuotationId
    ON dbo.InvoiceHeaders (QuotationId);
END;
GO

PRINT N'Quotation-to-invoice reference fields updated successfully.';
GO
