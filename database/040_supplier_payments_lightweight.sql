IF OBJECT_ID(N'dbo.SupplierPaymentHeaders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierPaymentHeaders
    (
        SupplierPaymentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPaymentHeaders PRIMARY KEY,
        PaymentNo NVARCHAR(30) NOT NULL,
        PaymentDate DATE NOT NULL,
        PurchaseOrderId INT NOT NULL,
        SupplierId INT NOT NULL,
        BranchId INT NULL,
        PaymentMethod NVARCHAR(20) NOT NULL CONSTRAINT DF_SupplierPaymentHeaders_PaymentMethod DEFAULT N'Transfer',
        ReferenceNo NVARCHAR(100) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Remark NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_SupplierPaymentHeaders_Status DEFAULT N'Posted',
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SupplierPaymentHeaders_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedDate DATETIME2 NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        PostedByUserId INT NULL,
        PostedDate DATETIME2 NULL,
        CancelledByUserId INT NULL,
        CancelledDate DATETIME2 NULL,
        CancelReason NVARCHAR(500) NULL
    );

    CREATE UNIQUE INDEX UX_SupplierPaymentHeaders_PaymentNo ON dbo.SupplierPaymentHeaders(PaymentNo);

    ALTER TABLE dbo.SupplierPaymentHeaders WITH CHECK
        ADD CONSTRAINT FK_SupplierPaymentHeaders_PurchaseOrders
            FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrderHeaders(PurchaseOrderId);

    ALTER TABLE dbo.SupplierPaymentHeaders WITH CHECK
        ADD CONSTRAINT FK_SupplierPaymentHeaders_Suppliers
            FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(SupplierId);

    ALTER TABLE dbo.SupplierPaymentHeaders WITH CHECK
        ADD CONSTRAINT FK_SupplierPaymentHeaders_Branches
            FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    ALTER TABLE dbo.SupplierPaymentHeaders WITH CHECK
        ADD CONSTRAINT FK_SupplierPaymentHeaders_CreatedByUser
            FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId);

    ALTER TABLE dbo.SupplierPaymentHeaders WITH CHECK
        ADD CONSTRAINT FK_SupplierPaymentHeaders_UpdatedByUser
            FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);

    ALTER TABLE dbo.SupplierPaymentHeaders WITH CHECK
        ADD CONSTRAINT FK_SupplierPaymentHeaders_PostedByUser
            FOREIGN KEY (PostedByUserId) REFERENCES dbo.Users(UserId);

    ALTER TABLE dbo.SupplierPaymentHeaders WITH CHECK
        ADD CONSTRAINT FK_SupplierPaymentHeaders_CancelledByUser
            FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId);
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
    (N'Purchasing.SupplierPayments.Menu', N'Access Supplier Payments menu', N'Menu Access'),
    (N'SupplierPayment.View', N'View supplier payments', N'Purchasing'),
    (N'SupplierPayment.Create', N'Create supplier payments', N'Purchasing'),
    (N'SupplierPayment.Cancel', N'Cancel supplier payments', N'Purchasing');

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
    (N'Admin', N'Purchasing.SupplierPayments.Menu'),
    (N'Admin', N'SupplierPayment.View'),
    (N'Admin', N'SupplierPayment.Create'),
    (N'Admin', N'SupplierPayment.Cancel'),
    (N'CentralAdmin', N'Purchasing.SupplierPayments.Menu'),
    (N'CentralAdmin', N'SupplierPayment.View'),
    (N'CentralAdmin', N'SupplierPayment.Create'),
    (N'CentralAdmin', N'SupplierPayment.Cancel'),
    (N'Executive', N'Purchasing.SupplierPayments.Menu'),
    (N'Executive', N'SupplierPayment.View'),
    (N'Executive', N'SupplierPayment.Create'),
    (N'Executive', N'SupplierPayment.Cancel');

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
