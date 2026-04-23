-- Branch foundation for branch-based inventory.
-- Safe to run multiple times.

IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Branches
    (
        BranchId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Branches PRIMARY KEY,
        BranchCode NVARCHAR(30) NOT NULL,
        BranchName NVARCHAR(150) NOT NULL,
        Address NVARCHAR(500) NULL,
        PhoneNumber NVARCHAR(50) NULL,
        Email NVARCHAR(256) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Branches_IsActive DEFAULT (1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Branches_CreatedDate DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_Branches_BranchCode ON dbo.Branches(BranchCode);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE BranchCode = N'MAIN')
BEGIN
    INSERT INTO dbo.Branches (BranchCode, BranchName, Address, PhoneNumber, Email, IsActive)
    VALUES (N'MAIN', N'Main Branch', NULL, NULL, NULL, 1);
END;
GO

IF COL_LENGTH('dbo.Users', 'BranchId') IS NULL
    ALTER TABLE dbo.Users ADD BranchId INT NULL;
GO

IF COL_LENGTH('dbo.Users', 'CanAccessAllBranches') IS NULL
    ALTER TABLE dbo.Users ADD CanAccessAllBranches BIT NOT NULL CONSTRAINT DF_Users_CanAccessAllBranches DEFAULT (0);
GO

DECLARE @MainBranchId INT = (SELECT TOP 1 BranchId FROM dbo.Branches WHERE BranchCode = N'MAIN');

UPDATE dbo.Users
SET BranchId = @MainBranchId
WHERE BranchId IS NULL;

UPDATE dbo.Users
SET CanAccessAllBranches = 1
WHERE Role = N'Admin';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Users_Branches_BranchId')
BEGIN
    ALTER TABLE dbo.Users
    ADD CONSTRAINT FK_Users_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_BranchId' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE INDEX IX_Users_BranchId ON dbo.Users(BranchId);
GO
