IF OBJECT_ID(N'dbo.BillingNoteHeaders', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.BillingNoteHeaders', 'SubtotalAmount') IS NULL
        ALTER TABLE dbo.BillingNoteHeaders ADD SubtotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_SubtotalAmount DEFAULT (0);

    IF COL_LENGTH('dbo.BillingNoteHeaders', 'DiscountAmount') IS NULL
        ALTER TABLE dbo.BillingNoteHeaders ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_DiscountAmount DEFAULT (0);

    IF COL_LENGTH('dbo.BillingNoteHeaders', 'VatAmount') IS NULL
        ALTER TABLE dbo.BillingNoteHeaders ADD VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_VatAmount DEFAULT (0);
END;
GO

IF OBJECT_ID(N'dbo.BillingNoteHeaders', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.BillingNoteHeaders
    SET
        SubtotalAmount = CASE
            WHEN ISNULL(SubtotalAmount, 0) = 0 AND ISNULL(DiscountAmount, 0) = 0 AND ISNULL(VatAmount, 0) = 0
                THEN TotalAmount
            ELSE SubtotalAmount
        END,
        DiscountAmount = ISNULL(DiscountAmount, 0),
        VatAmount = ISNULL(VatAmount, 0),
        BalanceAmount = CASE
            WHEN PaidAmount >= TotalAmount THEN 0
            ELSE TotalAmount - PaidAmount
        END;
END;
GO
