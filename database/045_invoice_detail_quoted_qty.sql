IF COL_LENGTH('dbo.InvoiceDetails', 'QuotedQty') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceDetails
        ADD QuotedQty decimal(18, 2) NULL;
END

IF COL_LENGTH('dbo.InvoiceDetails', 'QuotedQty') IS NOT NULL
BEGIN
    EXEC('
        UPDATE dbo.InvoiceDetails
        SET QuotedQty = Qty
        WHERE QuotedQty IS NULL;
    ');

    ALTER TABLE dbo.InvoiceDetails
        ALTER COLUMN QuotedQty decimal(18, 2) NOT NULL;
END
