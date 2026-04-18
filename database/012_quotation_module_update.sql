IF COL_LENGTH('dbo.QuotationHeaders', 'VatType') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD VatType NVARCHAR(10) NOT NULL CONSTRAINT DF_QuotationHeaders_VatType DEFAULT (N'NoVAT');
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'ExpiryDate') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD ExpiryDate DATE NULL;
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'ReferenceNo') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD ReferenceNo NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'DiscountMode') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD DiscountMode NVARCHAR(10) NOT NULL CONSTRAINT DF_QuotationHeaders_DiscountMode DEFAULT (N'Line');
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'HeaderDiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD HeaderDiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationHeaders_HeaderDiscountAmount DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'VatAmount') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationHeaders_VatAmount DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'CreatedDate') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_QuotationHeaders_CreatedDate DEFAULT (SYSUTCDATETIME());
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'UpdatedDate') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD UpdatedDate DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'ValidUntilDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders DROP COLUMN ValidUntilDate;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.QuotationDetails')
      AND name = N'Description'
      AND max_length < 1000
)
BEGIN
    ALTER TABLE dbo.QuotationDetails ALTER COLUMN Description NVARCHAR(1000) NULL;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.QuotationHeaders')
      AND name = N'SalespersonId'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.QuotationHeaders ALTER COLUMN SalespersonId INT NULL;
END;
GO

UPDATE dbo.QuotationHeaders
SET VatType = ISNULL(NULLIF(VatType, N''), N'NoVAT'),
    DiscountMode = ISNULL(NULLIF(DiscountMode, N''), N'Line'),
    VatAmount = ISNULL(VatAmount, 0),
    HeaderDiscountAmount = ISNULL(HeaderDiscountAmount, 0),
    UpdatedDate = ISNULL(UpdatedDate, SYSUTCDATETIME());
GO

PRINT N'Quotation module columns updated for discount mode, VAT, expiry date, reference no, header discount, and conversion workflow.';
GO
