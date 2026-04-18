IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'VatType') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders
        ADD VatType NVARCHAR(10) NOT NULL
            CONSTRAINT DF_PurchaseOrderHeaders_VatType DEFAULT (N'VAT');
END
GO

UPDATE dbo.PurchaseOrderHeaders
SET VatType = CASE
    WHEN VatAmount > 0 THEN N'VAT'
    ELSE N'NoVAT'
END
WHERE VatType IS NULL
   OR VatType NOT IN (N'VAT', N'NoVAT');
GO
