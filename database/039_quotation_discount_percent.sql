IF COL_LENGTH('dbo.QuotationHeaders', 'HeaderDiscountType') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD HeaderDiscountType NVARCHAR(10) NOT NULL
        CONSTRAINT DF_QuotationHeaders_HeaderDiscountType DEFAULT N'Amount';
END;
GO

IF COL_LENGTH('dbo.QuotationHeaders', 'HeaderDiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationHeaders
    ADD HeaderDiscountPercent DECIMAL(9,4) NOT NULL
        CONSTRAINT DF_QuotationHeaders_HeaderDiscountPercent DEFAULT 0;
END;
GO

IF COL_LENGTH('dbo.QuotationDetails', 'DiscountType') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationDetails
    ADD DiscountType NVARCHAR(10) NOT NULL
        CONSTRAINT DF_QuotationDetails_DiscountType DEFAULT N'Amount';
END;
GO

IF COL_LENGTH('dbo.QuotationDetails', 'DiscountPercent') IS NULL
BEGIN
    ALTER TABLE dbo.QuotationDetails
    ADD DiscountPercent DECIMAL(9,4) NOT NULL
        CONSTRAINT DF_QuotationDetails_DiscountPercent DEFAULT 0;
END;
GO
