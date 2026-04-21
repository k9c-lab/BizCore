-- Customer Claim / RMA Phase 3 supplier claim link and replacement support.
-- Safe to run multiple times after 022 and 023.

IF OBJECT_ID('dbo.SerialClaimLogs', 'U') IS NOT NULL
    AND OBJECT_ID('dbo.CustomerClaimHeaders', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SerialClaimLogs', 'CustomerClaimId') IS NULL
        ALTER TABLE dbo.SerialClaimLogs ADD CustomerClaimId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SerialClaimLogs_CustomerClaimId' AND object_id = OBJECT_ID(N'dbo.SerialClaimLogs'))
        CREATE INDEX IX_SerialClaimLogs_CustomerClaimId ON dbo.SerialClaimLogs(CustomerClaimId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SerialClaimLogs_CustomerClaimHeaders_CustomerClaimId')
        ALTER TABLE dbo.SerialClaimLogs ADD CONSTRAINT FK_SerialClaimLogs_CustomerClaimHeaders_CustomerClaimId FOREIGN KEY (CustomerClaimId) REFERENCES dbo.CustomerClaimHeaders(CustomerClaimId);
END;
GO
