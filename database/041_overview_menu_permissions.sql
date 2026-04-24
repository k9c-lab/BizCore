DECLARE @PermissionSeed TABLE
(
    Code NVARCHAR(80) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Module NVARCHAR(80) NOT NULL
);

INSERT INTO @PermissionSeed (Code, Name, Module)
VALUES
    (N'Dashboard.Menu', N'Access dashboard menu', N'Menu Access'),
    (N'FinancialOverview.Menu', N'Access Financial Overview menu', N'Menu Access'),
    (N'InventoryOverview.Menu', N'Access Inventory Overview menu', N'Menu Access');

MERGE dbo.Permissions AS target
USING @PermissionSeed AS source
    ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET Name = source.Name, Module = source.Module
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Module)
    VALUES (source.Code, source.Name, source.Module);
GO

DECLARE @GrantSeed TABLE
(
    RoleName NVARCHAR(30) NOT NULL,
    PermissionCode NVARCHAR(80) NOT NULL
);

INSERT INTO @GrantSeed (RoleName, PermissionCode)
VALUES
    (N'Admin', N'Dashboard.Menu'),
    (N'Admin', N'FinancialOverview.Menu'),
    (N'Admin', N'InventoryOverview.Menu'),
    (N'CentralAdmin', N'FinancialOverview.Menu'),
    (N'CentralAdmin', N'InventoryOverview.Menu'),
    (N'BranchAdmin', N'FinancialOverview.Menu'),
    (N'BranchAdmin', N'InventoryOverview.Menu'),
    (N'Sales', N'FinancialOverview.Menu'),
    (N'Warehouse', N'FinancialOverview.Menu'),
    (N'Warehouse', N'InventoryOverview.Menu'),
    (N'Executive', N'FinancialOverview.Menu'),
    (N'Executive', N'InventoryOverview.Menu');

INSERT INTO dbo.RolePermissions (RoleName, PermissionId)
SELECT seed.RoleName, permissions.PermissionId
FROM @GrantSeed seed
INNER JOIN dbo.Permissions permissions ON permissions.Code = seed.PermissionCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions existing
    WHERE existing.RoleName = seed.RoleName
      AND existing.PermissionId = permissions.PermissionId
);
GO

DELETE rolePermissions
FROM dbo.RolePermissions rolePermissions
INNER JOIN dbo.Permissions permissions ON permissions.PermissionId = rolePermissions.PermissionId
WHERE permissions.Code IN (N'Dashboard.Menu', N'Reports.Menu')
  AND rolePermissions.RoleName IN (N'CentralAdmin', N'BranchAdmin', N'Sales', N'Warehouse', N'Executive');
GO
