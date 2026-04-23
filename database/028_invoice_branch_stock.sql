-- Invoice branch stock support.
-- Run after 026_branch_foundation.sql and 027_branch_stock_foundation.sql.

IF OBJECT_ID(N'dbo.InvoiceHeaders', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.InvoiceHeaders', 'BranchId') IS NULL
        ALTER TABLE dbo.InvoiceHeaders ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_Branches_BranchId')
        ALTER TABLE dbo.InvoiceHeaders ADD CONSTRAINT FK_InvoiceHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceHeaders_BranchId' AND object_id = OBJECT_ID(N'dbo.InvoiceHeaders'))
        CREATE INDEX IX_InvoiceHeaders_BranchId ON dbo.InvoiceHeaders(BranchId);

    DECLARE @MainBranchId INT = (SELECT TOP 1 BranchId FROM dbo.Branches WHERE BranchCode = N'MAIN');

    IF @MainBranchId IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.InvoiceHeaders SET BranchId = @BranchId WHERE BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;
END;
GO
