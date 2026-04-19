IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD CreatedByUserId int NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'UpdatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD UpdatedByUserId int NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'ApprovedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD ApprovedByUserId int NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'ApprovedDate') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD ApprovedDate datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'CancelledByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD CancelledByUserId int NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'CancelledDate') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD CancelledDate datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'CancelReason') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD CancelReason nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderHeaders_CreatedByUsers')
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders WITH CHECK
    ADD CONSTRAINT FK_PurchaseOrderHeaders_CreatedByUsers
    FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderHeaders_UpdatedByUsers')
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders WITH CHECK
    ADD CONSTRAINT FK_PurchaseOrderHeaders_UpdatedByUsers
    FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users (UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderHeaders_ApprovedByUsers')
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders WITH CHECK
    ADD CONSTRAINT FK_PurchaseOrderHeaders_ApprovedByUsers
    FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users (UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderHeaders_CancelledByUsers')
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders WITH CHECK
    ADD CONSTRAINT FK_PurchaseOrderHeaders_CancelledByUsers
    FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users (UserId);
END;
GO

UPDATE dbo.PurchaseOrderHeaders
SET ApprovedDate = UpdatedDate
WHERE Status IN (N'Approved', N'PartiallyReceived', N'FullyReceived')
  AND ApprovedDate IS NULL
  AND UpdatedDate IS NOT NULL;
GO
