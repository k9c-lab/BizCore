IF COL_LENGTH('dbo.InvoiceHeaders', 'ReferenceLineSubtotal') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD ReferenceLineSubtotal decimal(18, 2) NULL;
END;

IF COL_LENGTH('dbo.InvoiceHeaders', 'ReferenceLineDiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD ReferenceLineDiscountAmount decimal(18, 2) NULL;
END;

IF COL_LENGTH('dbo.InvoiceHeaders', 'ReferenceLineVatAmount') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD ReferenceLineVatAmount decimal(18, 2) NULL;
END;

IF COL_LENGTH('dbo.InvoiceHeaders', 'ReferenceLineTotalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD ReferenceLineTotalAmount decimal(18, 2) NULL;
END;
