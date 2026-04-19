IF COL_LENGTH('dbo.Users', 'PasswordHash') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD PasswordHash nvarchar(300) NULL;
END;
GO

IF COL_LENGTH('dbo.Users', 'Role') IS NULL
BEGIN
    ALTER TABLE dbo.Users
    ADD Role nvarchar(30) NULL;
END;
GO

UPDATE dbo.Users
SET PasswordHash = N'PBKDF2-SHA256$100000$Qml6Q29yZUFkbWluU2FsdDEyMzQ1Ng==$q5v2GZ/ztGXmJmXF0qB/yRR3hvZIWG+Yh2oxi9GMapI='
WHERE PasswordHash IS NULL OR LTRIM(RTRIM(PasswordHash)) = N'';
GO

UPDATE dbo.Users
SET Role = CASE
    WHEN Username = N'admin' THEN N'Admin'
    WHEN Username LIKE N'%sales%' THEN N'Sales'
    WHEN Username LIKE N'%inventory%' THEN N'Warehouse'
    ELSE N'Viewer'
END
WHERE Role IS NULL OR LTRIM(RTRIM(Role)) = N'';
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Users_Role')
BEGIN
    ALTER TABLE dbo.Users
    ADD CONSTRAINT DF_Users_Role DEFAULT (N'Viewer') FOR Role;
END;
GO

ALTER TABLE dbo.Users
ALTER COLUMN PasswordHash nvarchar(300) NOT NULL;
GO

ALTER TABLE dbo.Users
ALTER COLUMN Role nvarchar(30) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, DisplayName, Email, PasswordHash, Role, IsActive)
    VALUES (
        N'admin',
        N'System Administrator',
        N'admin@bizcore.local',
        N'PBKDF2-SHA256$100000$Qml6Q29yZUFkbWluU2FsdDEyMzQ1Ng==$q5v2GZ/ztGXmJmXF0qB/yRR3hvZIWG+Yh2oxi9GMapI=',
        N'Admin',
        1
    );
END;
GO
