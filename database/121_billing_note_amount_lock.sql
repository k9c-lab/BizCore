IF COL_LENGTH(N'dbo.BillingNoteHeaders', N'IsAmountLocked') IS NULL
BEGIN
    ALTER TABLE dbo.BillingNoteHeaders ADD IsAmountLocked BIT NOT NULL DEFAULT 0;
END;
GO
