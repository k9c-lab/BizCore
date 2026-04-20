-- Adds UpdatedByUserId audit fields to Payment and Receipt modules.
-- Safe to run multiple times after 020_sales_finance_audit_fields.sql.

IF COL_LENGTH('dbo.PaymentHeaders', 'UpdatedByUserId') IS NULL
    ALTER TABLE dbo.PaymentHeaders ADD UpdatedByUserId INT NULL;
IF COL_LENGTH('dbo.ReceiptHeaders', 'UpdatedByUserId') IS NULL
    ALTER TABLE dbo.ReceiptHeaders ADD UpdatedByUserId INT NULL;
GO

DECLARE @DefaultUserId INT;

SELECT TOP (1) @DefaultUserId = UserId
FROM dbo.Users
WHERE Username = N'admin'
ORDER BY UserId;

IF @DefaultUserId IS NULL
BEGIN
    SELECT TOP (1) @DefaultUserId = UserId
    FROM dbo.Users
    WHERE IsActive = 1
    ORDER BY UserId;
END;

IF @DefaultUserId IS NOT NULL
BEGIN
    UPDATE dbo.PaymentHeaders
    SET UpdatedByUserId = @DefaultUserId
    WHERE UpdatedDate IS NOT NULL
      AND UpdatedByUserId IS NULL;

    UPDATE dbo.ReceiptHeaders
    SET UpdatedByUserId = @DefaultUserId
    WHERE UpdatedDate IS NOT NULL
      AND UpdatedByUserId IS NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentHeaders_Users_UpdatedByUserId')
    ALTER TABLE dbo.PaymentHeaders ADD CONSTRAINT FK_PaymentHeaders_Users_UpdatedByUserId FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceiptHeaders_Users_UpdatedByUserId')
    ALTER TABLE dbo.ReceiptHeaders ADD CONSTRAINT FK_ReceiptHeaders_Users_UpdatedByUserId FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);
GO
