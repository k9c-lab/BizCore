IF COL_LENGTH('dbo.ReceivingHeaders', 'UpdatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingHeaders ADD UpdatedByUserId int NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceivingHeaders_UpdatedByUsers')
BEGIN
    ALTER TABLE dbo.ReceivingHeaders WITH CHECK
    ADD CONSTRAINT FK_ReceivingHeaders_UpdatedByUsers
    FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users (UserId);
END;
GO
