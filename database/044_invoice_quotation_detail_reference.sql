IF COL_LENGTH('dbo.InvoiceDetails', 'QuotationDetailId') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceDetails
        ADD QuotationDetailId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InvoiceDetails_QuotationDetailId'
      AND object_id = OBJECT_ID('dbo.InvoiceDetails')
)
BEGIN
    CREATE INDEX IX_InvoiceDetails_QuotationDetailId
        ON dbo.InvoiceDetails (QuotationDetailId);
END
GO
