-- Add Inventory.StockAdjustments.Menu permission and assign to roles that have StockIssues access

DECLARE @PermissionSeed TABLE (Code NVARCHAR(80) NOT NULL, Name NVARCHAR(150) NOT NULL, Module NVARCHAR(80) NOT NULL);

INSERT INTO @PermissionSeed (Code, Name, Module)
VALUES
    (N'Inventory.StockAdjustments.Menu', N'Access Stock Adjustments menu', N'Menu Access');

MERGE INTO dbo.Permissions AS target
USING @PermissionSeed AS source ON target.Code = source.Code
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name, Module) VALUES (source.Code, source.Name, source.Module);

DECLARE @RolePermissionSeed TABLE (RoleName NVARCHAR(30) NOT NULL, PermissionCode NVARCHAR(80) NOT NULL);

INSERT INTO @RolePermissionSeed (RoleName, PermissionCode)
VALUES
    (N'Admin',       N'Inventory.StockAdjustments.Menu'),
    (N'BranchAdmin', N'Inventory.StockAdjustments.Menu'),
    (N'Warehouse',   N'Inventory.StockAdjustments.Menu');

INSERT INTO dbo.RolePermissions (RoleName, PermissionId)
SELECT seed.RoleName, p.PermissionId
FROM @RolePermissionSeed seed
INNER JOIN dbo.Permissions p ON p.Code = seed.PermissionCode
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions existing
    WHERE existing.RoleName = seed.RoleName
      AND existing.PermissionId = p.PermissionId
);
GO
