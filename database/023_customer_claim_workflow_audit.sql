-- Customer Claim / RMA Phase 2 workflow audit fields.
-- Safe to run multiple times after 022_customer_claim_module.sql.

IF OBJECT_ID('dbo.CustomerClaimHeaders', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ReceivedByUserId') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ReceivedByUserId INT NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'SentToSupplierByUserId') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD SentToSupplierByUserId INT NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ResolvedByUserId') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ResolvedByUserId INT NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ReturnedByUserId') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ReturnedByUserId INT NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ClosedByUserId') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ClosedByUserId INT NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ReceivedDate') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ReceivedDate DATETIME2 NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'SentToSupplierDate') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD SentToSupplierDate DATETIME2 NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ResolvedDate') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ResolvedDate DATETIME2 NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ReturnedDate') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ReturnedDate DATETIME2 NULL;

    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'ClosedDate') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD ClosedDate DATETIME2 NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CustomerClaimHeaders_Users_ReceivedByUserId')
        ALTER TABLE dbo.CustomerClaimHeaders ADD CONSTRAINT FK_CustomerClaimHeaders_Users_ReceivedByUserId FOREIGN KEY (ReceivedByUserId) REFERENCES dbo.Users(UserId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CustomerClaimHeaders_Users_SentToSupplierByUserId')
        ALTER TABLE dbo.CustomerClaimHeaders ADD CONSTRAINT FK_CustomerClaimHeaders_Users_SentToSupplierByUserId FOREIGN KEY (SentToSupplierByUserId) REFERENCES dbo.Users(UserId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CustomerClaimHeaders_Users_ResolvedByUserId')
        ALTER TABLE dbo.CustomerClaimHeaders ADD CONSTRAINT FK_CustomerClaimHeaders_Users_ResolvedByUserId FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users(UserId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CustomerClaimHeaders_Users_ReturnedByUserId')
        ALTER TABLE dbo.CustomerClaimHeaders ADD CONSTRAINT FK_CustomerClaimHeaders_Users_ReturnedByUserId FOREIGN KEY (ReturnedByUserId) REFERENCES dbo.Users(UserId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CustomerClaimHeaders_Users_ClosedByUserId')
        ALTER TABLE dbo.CustomerClaimHeaders ADD CONSTRAINT FK_CustomerClaimHeaders_Users_ClosedByUserId FOREIGN KEY (ClosedByUserId) REFERENCES dbo.Users(UserId);
END;
GO
