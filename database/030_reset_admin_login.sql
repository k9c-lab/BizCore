-- Emergency admin login reset.
-- Resets/creates admin with password: Admin@12345

DECLARE @AdminHash NVARCHAR(300) = N'PBKDF2-SHA256$100000$Qml6Q29yZUFkbWluU2FsdDEyMzQ1Ng==$q5v2GZ/ztGXmJmXF0qB/yRR3hvZIWG+Yh2oxi9GMapI=';
DECLARE @MainBranchId INT = NULL;

IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    SELECT TOP 1 @MainBranchId = BranchId
    FROM dbo.Branches
    WHERE BranchCode = N'MAIN'
    ORDER BY BranchId;
END;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    UPDATE dbo.Users
    SET
        DisplayName = N'System Administrator',
        Email = N'admin@bizcore.local',
        PasswordHash = @AdminHash,
        Role = N'Admin',
        IsActive = 1
    WHERE Username = N'admin';

    IF COL_LENGTH('dbo.Users', 'BranchId') IS NOT NULL AND @MainBranchId IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.Users SET BranchId = @BranchId WHERE Username = N''admin'';',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;

    IF COL_LENGTH('dbo.Users', 'CanAccessAllBranches') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.Users SET CanAccessAllBranches = 1 WHERE Username = N''admin'';';
    END;
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Username, DisplayName, Email, PasswordHash, Role, IsActive)
    VALUES (N'admin', N'System Administrator', N'admin@bizcore.local', @AdminHash, N'Admin', 1);

    IF COL_LENGTH('dbo.Users', 'BranchId') IS NOT NULL AND @MainBranchId IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.Users SET BranchId = @BranchId WHERE Username = N''admin'';',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;

    IF COL_LENGTH('dbo.Users', 'CanAccessAllBranches') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.Users SET CanAccessAllBranches = 1 WHERE Username = N''admin'';';
    END;
END;
GO

SELECT Username, DisplayName, Email, Role, IsActive
FROM dbo.Users
WHERE Username = N'admin';
GO
