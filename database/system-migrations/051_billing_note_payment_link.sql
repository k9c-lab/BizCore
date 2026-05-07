IF OBJECT_ID(N'dbo.BillingNoteHeaders', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.BillingNoteHeaders', 'PaidAmount') IS NULL
        ALTER TABLE dbo.BillingNoteHeaders ADD PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_PaidAmount DEFAULT (0);

    IF COL_LENGTH('dbo.BillingNoteHeaders', 'BalanceAmount') IS NULL
        ALTER TABLE dbo.BillingNoteHeaders ADD BalanceAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_BalanceAmount DEFAULT (0);
END;
GO

IF OBJECT_ID(N'dbo.BillingNoteHeaders', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.BillingNoteHeaders', 'PaidAmount') IS NOT NULL
   AND COL_LENGTH('dbo.BillingNoteHeaders', 'BalanceAmount') IS NOT NULL
BEGIN
    UPDATE dbo.BillingNoteHeaders
    SET BalanceAmount = CASE
            WHEN TotalAmount - PaidAmount < 0 THEN 0
            ELSE TotalAmount - PaidAmount
        END;
END;
GO

IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PaymentHeaders', 'BillingNoteId') IS NULL
        ALTER TABLE dbo.PaymentHeaders ADD BillingNoteId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentHeaders_BillingNoteHeaders_BillingNoteId')
        ALTER TABLE dbo.PaymentHeaders
        ADD CONSTRAINT FK_PaymentHeaders_BillingNoteHeaders_BillingNoteId
            FOREIGN KEY (BillingNoteId) REFERENCES dbo.BillingNoteHeaders (BillingNoteId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentHeaders_BillingNoteId' AND object_id = OBJECT_ID(N'dbo.PaymentHeaders'))
        CREATE INDEX IX_PaymentHeaders_BillingNoteId ON dbo.PaymentHeaders (BillingNoteId);
END;
GO
