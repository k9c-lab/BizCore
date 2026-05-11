IF OBJECT_ID(N'dbo.CashSaleHeaders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashSaleHeaders
    (
        CashSaleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashSaleHeaders PRIMARY KEY,
        CashSaleNo NVARCHAR(30) NOT NULL,
        CashSaleDate DATE NOT NULL,
        CustomerId INT NOT NULL,
        SalespersonId INT NULL,
        BranchId INT NULL,
        PriceLevelId INT NULL,
        ReferenceNo NVARCHAR(50) NULL,
        Remark NVARCHAR(500) NULL,
        Subtotal DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CashSaleHeaders_Subtotal DEFAULT (0),
        DiscountAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CashSaleHeaders_DiscountAmount DEFAULT (0),
        VatType NVARCHAR(10) NOT NULL CONSTRAINT DF_CashSaleHeaders_VatType DEFAULT (N'VAT'),
        VatAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CashSaleHeaders_VatAmount DEFAULT (0),
        TotalAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CashSaleHeaders_TotalAmount DEFAULT (0),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_CashSaleHeaders_Status DEFAULT (N'Draft'),
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_CashSaleHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        IssuedByUserId INT NULL,
        IssuedDate DATETIME2 NULL,
        CancelledByUserId INT NULL,
        CancelledDate DATETIME2 NULL,
        CancelReason NVARCHAR(500) NULL,
        CONSTRAINT UQ_CashSaleHeaders_CashSaleNo UNIQUE (CashSaleNo),
        CONSTRAINT FK_CashSaleHeaders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_CashSaleHeaders_Salespersons FOREIGN KEY (SalespersonId) REFERENCES dbo.Salespersons(SalespersonId),
        CONSTRAINT FK_CashSaleHeaders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId),
        CONSTRAINT FK_CashSaleHeaders_PriceLevels FOREIGN KEY (PriceLevelId) REFERENCES dbo.PriceLevels(PriceLevelId),
        CONSTRAINT FK_CashSaleHeaders_CreatedUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_CashSaleHeaders_UpdatedUsers FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_CashSaleHeaders_IssuedUsers FOREIGN KEY (IssuedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_CashSaleHeaders_CancelledUsers FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId)
    );
END;

IF OBJECT_ID(N'dbo.CashSaleDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashSaleDetails
    (
        CashSaleDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashSaleDetails PRIMARY KEY,
        CashSaleId INT NOT NULL,
        LineNumber INT NOT NULL,
        ItemId INT NOT NULL,
        Qty DECIMAL(18, 2) NOT NULL,
        UnitPrice DECIMAL(18, 2) NOT NULL,
        DiscountAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CashSaleDetails_DiscountAmount DEFAULT (0),
        LineTotal DECIMAL(18, 2) NOT NULL CONSTRAINT DF_CashSaleDetails_LineTotal DEFAULT (0),
        Remark NVARCHAR(500) NULL,
        CustomerWarrantyStartDate DATE NULL,
        CustomerWarrantyEndDate DATE NULL,
        CONSTRAINT FK_CashSaleDetails_Headers FOREIGN KEY (CashSaleId) REFERENCES dbo.CashSaleHeaders(CashSaleId) ON DELETE CASCADE,
        CONSTRAINT FK_CashSaleDetails_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId)
    );
END;

IF OBJECT_ID(N'dbo.CashSaleSerials', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashSaleSerials
    (
        CashSaleSerialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashSaleSerials PRIMARY KEY,
        CashSaleDetailId INT NOT NULL,
        SerialId INT NOT NULL,
        CONSTRAINT FK_CashSaleSerials_Details FOREIGN KEY (CashSaleDetailId) REFERENCES dbo.CashSaleDetails(CashSaleDetailId) ON DELETE CASCADE,
        CONSTRAINT FK_CashSaleSerials_Serials FOREIGN KEY (SerialId) REFERENCES dbo.SerialNumbers(SerialId)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashSaleSerials_CashSaleDetailId_SerialId' AND object_id = OBJECT_ID(N'dbo.CashSaleSerials'))
BEGIN
    CREATE UNIQUE INDEX IX_CashSaleSerials_CashSaleDetailId_SerialId
        ON dbo.CashSaleSerials(CashSaleDetailId, SerialId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CashSaleSerials_SerialId' AND object_id = OBJECT_ID(N'dbo.CashSaleSerials'))
BEGIN
    CREATE UNIQUE INDEX IX_CashSaleSerials_SerialId
        ON dbo.CashSaleSerials(SerialId);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Code = N'Sales.CashSales.Menu')
BEGIN
    INSERT INTO dbo.Permissions(Code, Name, Module)
    VALUES (N'Sales.CashSales.Menu', N'Access Cash Sales menu', N'Menu Access');
END;

INSERT INTO dbo.RolePermissions(RoleName, PermissionId)
SELECT roles.RoleName, p.PermissionId
FROM (VALUES (N'Admin'), (N'CentralAdmin'), (N'BranchAdmin'), (N'Sales'), (N'Executive')) AS roles(RoleName)
INNER JOIN dbo.Permissions p ON p.Code = N'Sales.CashSales.Menu'
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions rp
    WHERE rp.RoleName = roles.RoleName
      AND rp.PermissionId = p.PermissionId
);
