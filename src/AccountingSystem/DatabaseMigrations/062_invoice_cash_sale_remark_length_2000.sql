IF COL_LENGTH('dbo.InvoiceHeaders', 'Remark') IS NOT NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ALTER COLUMN Remark nvarchar(2000) NULL;
END;

IF COL_LENGTH('dbo.InvoiceDetails', 'Remark') IS NOT NULL
BEGIN
    ALTER TABLE dbo.InvoiceDetails ALTER COLUMN Remark nvarchar(2000) NULL;
END;

IF COL_LENGTH('dbo.CashSaleHeaders', 'Remark') IS NOT NULL
BEGIN
    ALTER TABLE dbo.CashSaleHeaders ALTER COLUMN Remark nvarchar(2000) NULL;
END;

IF COL_LENGTH('dbo.CashSaleDetails', 'Remark') IS NOT NULL
BEGIN
    ALTER TABLE dbo.CashSaleDetails ALTER COLUMN Remark nvarchar(2000) NULL;
END;
