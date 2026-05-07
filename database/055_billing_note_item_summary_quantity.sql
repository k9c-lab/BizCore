IF OBJECT_ID(N'dbo.BillingNoteLines', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.BillingNoteLines', 'Quantity') IS NULL
    BEGIN
        ALTER TABLE dbo.BillingNoteLines
        ADD Quantity DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_BillingNoteLines_Quantity DEFAULT (0);
    END;
END;
GO

IF OBJECT_ID(N'dbo.BillingNoteLines', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.BillingNoteLines', 'Quantity') IS NOT NULL
BEGIN
    UPDATE dbo.BillingNoteLines
    SET Quantity = CASE
        WHEN Quantity = 0 THEN CONVERT(DECIMAL(18,2), InvoiceCount)
        ELSE Quantity
    END;
END;
GO
