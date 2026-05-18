IF OBJECT_ID(N'dbo.ReadingDoctors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReadingDoctors
    (
        ReadingDoctorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReadingDoctors PRIMARY KEY,
        DoctorCode NVARCHAR(30) NOT NULL,
        DoctorName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReadingDoctors_IsActive DEFAULT (1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ReadingDoctors_DoctorCode' AND object_id = OBJECT_ID(N'dbo.ReadingDoctors'))
BEGIN
    CREATE UNIQUE INDEX UX_ReadingDoctors_DoctorCode
        ON dbo.ReadingDoctors (DoctorCode);
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'ReadingDoctorId') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD ReadingDoctorId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_ReadingDoctors_ReadingDoctorId')
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD CONSTRAINT FK_InvoiceHeaders_ReadingDoctors_ReadingDoctorId
        FOREIGN KEY (ReadingDoctorId) REFERENCES dbo.ReadingDoctors (ReadingDoctorId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Code = N'MasterData.ReadingDoctors.Menu')
BEGIN
    INSERT INTO dbo.Permissions (Code, Name, Module)
    VALUES (N'MasterData.ReadingDoctors.Menu', N'Access Reading Doctors menu', N'Menu Access');
END;
GO

INSERT INTO dbo.RolePermissions (RoleName, PermissionId)
SELECT v.RoleName, p.PermissionId
FROM (VALUES
    (N'Admin'),
    (N'CentralAdmin'),
    (N'BranchAdmin')
) AS v(RoleName)
CROSS JOIN dbo.Permissions p
WHERE p.Code = N'MasterData.ReadingDoctors.Menu'
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.RolePermissions rp
      WHERE rp.RoleName = v.RoleName
        AND rp.PermissionId = p.PermissionId
  );
GO
