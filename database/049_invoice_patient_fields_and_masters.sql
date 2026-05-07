IF OBJECT_ID(N'dbo.TreatmentRights', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TreatmentRights
    (
        TreatmentRightId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TreatmentRights PRIMARY KEY,
        TreatmentRightCode NVARCHAR(30) NOT NULL,
        TreatmentRightName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TreatmentRights_IsActive DEFAULT (1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_TreatmentRights_TreatmentRightCode' AND object_id = OBJECT_ID(N'dbo.TreatmentRights'))
BEGIN
    CREATE UNIQUE INDEX UX_TreatmentRights_TreatmentRightCode
        ON dbo.TreatmentRights (TreatmentRightCode);
END;
GO

IF OBJECT_ID(N'dbo.ReferringDoctors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReferringDoctors
    (
        ReferringDoctorId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReferringDoctors PRIMARY KEY,
        DoctorCode NVARCHAR(30) NOT NULL,
        DoctorName NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReferringDoctors_IsActive DEFAULT (1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ReferringDoctors_DoctorCode' AND object_id = OBJECT_ID(N'dbo.ReferringDoctors'))
BEGIN
    CREATE UNIQUE INDEX UX_ReferringDoctors_DoctorCode
        ON dbo.ReferringDoctors (DoctorCode);
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'PatientFullName') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD PatientFullName NVARCHAR(200) NULL;
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'PatientAge') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD PatientAge INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'PatientGender') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD PatientGender NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'PatientHn') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD PatientHn NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'TreatmentRightId') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD TreatmentRightId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'PatientWard') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD PatientWard NVARCHAR(100) NULL;
END;
GO

IF COL_LENGTH(N'dbo.InvoiceHeaders', N'ReferringDoctorId') IS NULL
BEGIN
    ALTER TABLE dbo.InvoiceHeaders ADD ReferringDoctorId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_TreatmentRights_TreatmentRightId')
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD CONSTRAINT FK_InvoiceHeaders_TreatmentRights_TreatmentRightId
        FOREIGN KEY (TreatmentRightId) REFERENCES dbo.TreatmentRights (TreatmentRightId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_ReferringDoctors_ReferringDoctorId')
BEGIN
    ALTER TABLE dbo.InvoiceHeaders
        ADD CONSTRAINT FK_InvoiceHeaders_ReferringDoctors_ReferringDoctorId
        FOREIGN KEY (ReferringDoctorId) REFERENCES dbo.ReferringDoctors (ReferringDoctorId);
END;
GO

DECLARE @PermissionSeed TABLE
(
    Code NVARCHAR(80) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Module NVARCHAR(80) NOT NULL
);

INSERT INTO @PermissionSeed (Code, Name, Module)
VALUES
    (N'MasterData.TreatmentRights.Menu', N'Access Treatment Rights menu', N'Menu Access'),
    (N'MasterData.ReferringDoctors.Menu', N'Access Referring Doctors menu', N'Menu Access');

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
    (N'Admin', N'MasterData.TreatmentRights.Menu'),
    (N'Admin', N'MasterData.ReferringDoctors.Menu'),
    (N'CentralAdmin', N'MasterData.TreatmentRights.Menu'),
    (N'CentralAdmin', N'MasterData.ReferringDoctors.Menu'),
    (N'BranchAdmin', N'MasterData.TreatmentRights.Menu'),
    (N'BranchAdmin', N'MasterData.ReferringDoctors.Menu');

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
