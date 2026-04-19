IF COL_LENGTH('dbo.ReceivingHeaders', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingHeaders ADD CreatedByUserId int NULL;
END;
GO

IF COL_LENGTH('dbo.ReceivingHeaders', 'PostedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingHeaders ADD PostedByUserId int NULL;
END;
GO

IF COL_LENGTH('dbo.ReceivingHeaders', 'PostedDate') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingHeaders ADD PostedDate datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.ReceivingHeaders', 'CancelledByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingHeaders ADD CancelledByUserId int NULL;
END;
GO

IF COL_LENGTH('dbo.ReceivingHeaders', 'CancelledDate') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingHeaders ADD CancelledDate datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.ReceivingHeaders', 'CancelReason') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingHeaders ADD CancelReason nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceivingHeaders_CreatedByUsers')
BEGIN
    ALTER TABLE dbo.ReceivingHeaders WITH CHECK
    ADD CONSTRAINT FK_ReceivingHeaders_CreatedByUsers
    FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceivingHeaders_PostedByUsers')
BEGIN
    ALTER TABLE dbo.ReceivingHeaders WITH CHECK
    ADD CONSTRAINT FK_ReceivingHeaders_PostedByUsers
    FOREIGN KEY (PostedByUserId) REFERENCES dbo.Users (UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceivingHeaders_CancelledByUsers')
BEGIN
    ALTER TABLE dbo.ReceivingHeaders WITH CHECK
    ADD CONSTRAINT FK_ReceivingHeaders_CancelledByUsers
    FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users (UserId);
END;
GO

UPDATE dbo.ReceivingHeaders
SET PostedDate = CreatedDate
WHERE Status = N'Posted' AND PostedDate IS NULL;
GO

DECLARE @AuditUserId int;

SELECT @AuditUserId = UserId
FROM dbo.Users
WHERE Username = N'admin';

IF @AuditUserId IS NULL
BEGIN
    SELECT TOP (1) @AuditUserId = UserId
    FROM dbo.Users
    WHERE IsActive = 1
    ORDER BY UserId;
END;

IF @AuditUserId IS NOT NULL
BEGIN
    UPDATE dbo.ReceivingHeaders
    SET CreatedByUserId = @AuditUserId
    WHERE CreatedByUserId IS NULL;

    UPDATE dbo.ReceivingHeaders
    SET PostedByUserId = @AuditUserId
    WHERE Status = N'Posted' AND PostedByUserId IS NULL;

    UPDATE dbo.ReceivingHeaders
    SET CancelledByUserId = @AuditUserId
    WHERE Status = N'Cancelled' AND CancelledByUserId IS NULL;
END;
GO
