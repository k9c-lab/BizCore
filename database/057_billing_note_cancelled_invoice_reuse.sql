IF OBJECT_ID(N'dbo.BillingNoteInvoices', N'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_BillingNoteInvoices_InvoiceId'
          AND object_id = OBJECT_ID(N'dbo.BillingNoteInvoices')
    )
    BEGIN
        DROP INDEX UX_BillingNoteInvoices_InvoiceId ON dbo.BillingNoteInvoices;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_BillingNoteInvoices_InvoiceId'
          AND object_id = OBJECT_ID(N'dbo.BillingNoteInvoices')
    )
    BEGIN
        CREATE INDEX IX_BillingNoteInvoices_InvoiceId
            ON dbo.BillingNoteInvoices (InvoiceId);
    END;
END;
GO
