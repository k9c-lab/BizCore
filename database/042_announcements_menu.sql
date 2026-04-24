IF OBJECT_ID(N'dbo.Announcements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Announcements
    (
        AnnouncementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Announcements PRIMARY KEY,
        Title NVARCHAR(150) NOT NULL,
        Message NVARCHAR(4000) NOT NULL,
        PublishFromDate DATE NULL,
        PublishToDate DATE NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Announcements_IsActive DEFAULT(1),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Announcements_CreatedDate DEFAULT(SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Announcements_CreatedByUser')
BEGIN
    ALTER TABLE dbo.Announcements WITH CHECK
    ADD CONSTRAINT FK_Announcements_CreatedByUser
    FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Announcements_UpdatedByUser')
BEGIN
    ALTER TABLE dbo.Announcements WITH CHECK
    ADD CONSTRAINT FK_Announcements_UpdatedByUser
    FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);
END;
GO

MERGE dbo.Permissions AS target
USING (VALUES (N'Announcements.Menu', N'Access Announcements menu', N'Menu Access')) AS source (Code, Name, Module)
    ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET Name = source.Name, Module = source.Module
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Module)
    VALUES (source.Code, source.Name, source.Module);
GO

INSERT INTO dbo.RolePermissions (RoleName, PermissionId)
SELECT seed.RoleName, permission.PermissionId
FROM (VALUES (N'Admin'), (N'Executive')) AS seed (RoleName)
INNER JOIN dbo.Permissions permission ON permission.Code = N'Announcements.Menu'
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions existing
    WHERE existing.RoleName = seed.RoleName
      AND existing.PermissionId = permission.PermissionId
);
GO
