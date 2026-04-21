-- Supplier Claim Resolution / Receiving fields.
-- Safe to run multiple times after supplier claim scripts.

IF OBJECT_ID('dbo.SerialClaimLogs', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SerialClaimLogs', 'SupplierReplacementSerialId') IS NULL
        ALTER TABLE dbo.SerialClaimLogs ADD SupplierReplacementSerialId INT NULL;

    IF COL_LENGTH('dbo.SerialClaimLogs', 'ResultType') IS NULL
        ALTER TABLE dbo.SerialClaimLogs ADD ResultType NVARCHAR(30) NULL;

    IF COL_LENGTH('dbo.SerialClaimLogs', 'SentDate') IS NULL
        ALTER TABLE dbo.SerialClaimLogs ADD SentDate DATETIME2 NULL;

    IF COL_LENGTH('dbo.SerialClaimLogs', 'ReceivedDate') IS NULL
        ALTER TABLE dbo.SerialClaimLogs ADD ReceivedDate DATETIME2 NULL;

    IF COL_LENGTH('dbo.SerialClaimLogs', 'ClosedDate') IS NULL
        ALTER TABLE dbo.SerialClaimLogs ADD ClosedDate DATETIME2 NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SerialClaimLogs_SupplierReplacementSerialId' AND object_id = OBJECT_ID(N'dbo.SerialClaimLogs'))
        CREATE INDEX IX_SerialClaimLogs_SupplierReplacementSerialId ON dbo.SerialClaimLogs(SupplierReplacementSerialId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SerialClaimLogs_SerialNumbers_SupplierReplacementSerialId')
        ALTER TABLE dbo.SerialClaimLogs ADD CONSTRAINT FK_SerialClaimLogs_SerialNumbers_SupplierReplacementSerialId FOREIGN KEY (SupplierReplacementSerialId) REFERENCES dbo.SerialNumbers(SerialId);
END;
GO
