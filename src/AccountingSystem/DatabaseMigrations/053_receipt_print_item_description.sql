IF COL_LENGTH('dbo.ReceiptHeaders', 'PrintItemDescription') IS NULL
BEGIN
    ALTER TABLE dbo.ReceiptHeaders
    ADD PrintItemDescription NVARCHAR(1000) NULL;
END;
