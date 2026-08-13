-- Migration 117: Invoice Audit Log
-- Stores human-readable change history for invoice edits, issue, and cancellation

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'InvoiceAuditLogs')
BEGIN
    CREATE TABLE InvoiceAuditLogs (
        AuditLogId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceId       INT             NOT NULL REFERENCES InvoiceHeaders(InvoiceId) ON DELETE CASCADE,
        UserId          INT             NULL,
        UserName        NVARCHAR(100)   NOT NULL DEFAULT '',
        OccurredAt      DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        Description     NVARCHAR(1000)  NOT NULL
    );

    CREATE INDEX IX_InvoiceAuditLogs_InvoiceId ON InvoiceAuditLogs(InvoiceId);
END;
