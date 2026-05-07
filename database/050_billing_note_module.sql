IF OBJECT_ID(N'dbo.BillingNoteHeaders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingNoteHeaders
    (
        BillingNoteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BillingNoteHeaders PRIMARY KEY,
        BillingNoteNo NVARCHAR(30) NOT NULL,
        BillingNoteDate DATE NOT NULL,
        CustomerId INT NOT NULL,
        BranchId INT NULL,
        SummaryMode NVARCHAR(30) NOT NULL CONSTRAINT DF_BillingNoteHeaders_SummaryMode DEFAULT (N'TreatmentRight'),
        InvoiceCount INT NOT NULL CONSTRAINT DF_BillingNoteHeaders_InvoiceCount DEFAULT (0),
        SubtotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_SubtotalAmount DEFAULT (0),
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_DiscountAmount DEFAULT (0),
        VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_VatAmount DEFAULT (0),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_TotalAmount DEFAULT (0),
        PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_PaidAmount DEFAULT (0),
        BalanceAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteHeaders_BalanceAmount DEFAULT (0),
        Remark NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_BillingNoteHeaders_Status DEFAULT (N'Issued'),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_BillingNoteHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        CancelledByUserId INT NULL,
        CancelledDate DATETIME2 NULL,
        CancelReason NVARCHAR(500) NULL,
        CONSTRAINT UX_BillingNoteHeaders_BillingNoteNo UNIQUE (BillingNoteNo),
        CONSTRAINT FK_BillingNoteHeaders_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
        CONSTRAINT FK_BillingNoteHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches (BranchId),
        CONSTRAINT FK_BillingNoteHeaders_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_BillingNoteHeaders_Users_UpdatedByUserId FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_BillingNoteHeaders_Users_CancelledByUserId FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users (UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.BillingNoteInvoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingNoteInvoices
    (
        BillingNoteInvoiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BillingNoteInvoices PRIMARY KEY,
        BillingNoteId INT NOT NULL,
        InvoiceId INT NOT NULL,
        BilledAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteInvoices_BilledAmount DEFAULT (0),
        CONSTRAINT FK_BillingNoteInvoices_BillingNoteHeaders_BillingNoteId FOREIGN KEY (BillingNoteId) REFERENCES dbo.BillingNoteHeaders (BillingNoteId),
        CONSTRAINT FK_BillingNoteInvoices_InvoiceHeaders_InvoiceId FOREIGN KEY (InvoiceId) REFERENCES dbo.InvoiceHeaders (InvoiceId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BillingNoteInvoices_InvoiceId' AND object_id = OBJECT_ID(N'dbo.BillingNoteInvoices'))
BEGIN
    CREATE INDEX IX_BillingNoteInvoices_InvoiceId
        ON dbo.BillingNoteInvoices (InvoiceId);
END;
GO

IF OBJECT_ID(N'dbo.BillingNoteLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillingNoteLines
    (
        BillingNoteLineId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BillingNoteLines PRIMARY KEY,
        BillingNoteId INT NOT NULL,
        LineNumber INT NOT NULL,
        SummaryType NVARCHAR(30) NOT NULL CONSTRAINT DF_BillingNoteLines_SummaryType DEFAULT (N'TreatmentRight'),
        TreatmentRightId INT NULL,
        Description NVARCHAR(200) NOT NULL,
        Quantity DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteLines_Quantity DEFAULT (0),
        InvoiceCount INT NOT NULL CONSTRAINT DF_BillingNoteLines_InvoiceCount DEFAULT (0),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BillingNoteLines_TotalAmount DEFAULT (0),
        CONSTRAINT FK_BillingNoteLines_BillingNoteHeaders_BillingNoteId FOREIGN KEY (BillingNoteId) REFERENCES dbo.BillingNoteHeaders (BillingNoteId),
        CONSTRAINT FK_BillingNoteLines_TreatmentRights_TreatmentRightId FOREIGN KEY (TreatmentRightId) REFERENCES dbo.TreatmentRights (TreatmentRightId)
    );
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
    (N'Sales.BillingNotes.Menu', N'Access Billing Notes menu', N'Menu Access');

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
    (N'Admin', N'Sales.BillingNotes.Menu'),
    (N'CentralAdmin', N'Sales.BillingNotes.Menu'),
    (N'BranchAdmin', N'Sales.BillingNotes.Menu'),
    (N'Sales', N'Sales.BillingNotes.Menu'),
    (N'Executive', N'Sales.BillingNotes.Menu');

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
