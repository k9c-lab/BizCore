DECLARE @PermissionSeed TABLE
(
    Code NVARCHAR(80) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Module NVARCHAR(80) NOT NULL
);

INSERT INTO @PermissionSeed (Code, Name, Module)
VALUES
    (N'MasterData.PriceLevels.Menu', N'Access Price Levels menu', N'Menu Access'),
    (N'MasterData.Settings.Menu', N'Access Settings menu', N'Menu Access');

MERGE dbo.Permissions AS target
USING @PermissionSeed AS source
    ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET Name = source.Name, Module = source.Module
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Module)
    VALUES (source.Code, source.Name, source.Module);
GO

DECLARE @RolePermissionSeed TABLE
(
    RoleName NVARCHAR(30) NOT NULL,
    PermissionCode NVARCHAR(80) NOT NULL
);

INSERT INTO @RolePermissionSeed (RoleName, PermissionCode)
VALUES
    (N'Admin', N'MasterData.PriceLevels.Menu'),
    (N'Admin', N'MasterData.Settings.Menu');

INSERT INTO dbo.RolePermissions (RoleName, PermissionId)
SELECT seed.RoleName, permissions.PermissionId
FROM @RolePermissionSeed seed
INNER JOIN dbo.Permissions permissions ON permissions.Code = seed.PermissionCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions existing
    WHERE existing.RoleName = seed.RoleName
      AND existing.PermissionId = permissions.PermissionId
);
GO
