-- Migration 119: Seed Patient menu permissions and assign to BranchAdmin

DECLARE @PermissionSeed TABLE (Code NVARCHAR(80) NOT NULL, Name NVARCHAR(150) NOT NULL, Module NVARCHAR(80) NOT NULL);

INSERT INTO @PermissionSeed (Code, Name, Module)
VALUES
    (N'Patient.Visits.Menu',      N'Access Patient Visits (Registration) menu', N'Menu Access'),
    (N'MasterData.Patients.Menu', N'Access Patients master data menu',           N'Menu Access');

MERGE INTO dbo.Permissions AS target
USING @PermissionSeed AS source ON target.Code = source.Code
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name, Module) VALUES (source.Code, source.Name, source.Module);

DECLARE @RolePermissionSeed TABLE (RoleName NVARCHAR(30) NOT NULL, PermissionCode NVARCHAR(80) NOT NULL);

INSERT INTO @RolePermissionSeed (RoleName, PermissionCode)
VALUES
    (N'BranchAdmin', N'Patient.Visits.Menu'),
    (N'BranchAdmin', N'MasterData.Patients.Menu');

INSERT INTO dbo.RolePermissions (RoleName, PermissionId)
SELECT seed.RoleName, p.PermissionId
FROM @RolePermissionSeed seed
INNER JOIN dbo.Permissions p ON p.Code = seed.PermissionCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermissions existing
    WHERE existing.RoleName = seed.RoleName
      AND existing.PermissionId = p.PermissionId
);
GO
