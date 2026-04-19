CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    DisplayName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(256) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME())
);
GO

CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users (Username);
GO

CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email) WHERE Email IS NOT NULL;
GO

CREATE TABLE dbo.Customers
(
    CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerCode NVARCHAR(30) NOT NULL,
    CustomerName NVARCHAR(200) NOT NULL,
    TaxId NVARCHAR(30) NULL,
    Address NVARCHAR(500) NULL,
    PhoneNumber NVARCHAR(50) NULL,
    Email NVARCHAR(256) NULL,
    CreditLimit DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customers_CreditLimit DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1)
);
GO

CREATE UNIQUE INDEX UX_Customers_Code ON dbo.Customers (CustomerCode);
GO

CREATE UNIQUE INDEX UX_Customers_Email ON dbo.Customers (Email) WHERE Email IS NOT NULL;
GO

CREATE TABLE dbo.Suppliers
(
    SupplierId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    SupplierCode NVARCHAR(30) NOT NULL,
    SupplierName NVARCHAR(200) NOT NULL,
    TaxId NVARCHAR(30) NULL,
    Address NVARCHAR(500) NULL,
    PhoneNumber NVARCHAR(50) NULL,
    Email NVARCHAR(256) NULL,
    CreditLimit DECIMAL(18,2) NOT NULL CONSTRAINT DF_Suppliers_CreditLimit DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1)
);
GO

CREATE UNIQUE INDEX UX_Suppliers_Code ON dbo.Suppliers (SupplierCode);
GO

CREATE UNIQUE INDEX UX_Suppliers_Email ON dbo.Suppliers (Email) WHERE Email IS NOT NULL;
GO

CREATE TABLE dbo.Salespersons
(
    SalespersonId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Salespersons PRIMARY KEY,
    SalespersonCode NVARCHAR(30) NOT NULL,
    SalespersonName NVARCHAR(200) NOT NULL,
    PhoneNumber NVARCHAR(50) NULL,
    Email NVARCHAR(256) NULL,
    CommissionRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_Salespersons_CommissionRate DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_Salespersons_IsActive DEFAULT (1)
);
GO

CREATE UNIQUE INDEX UX_Salespersons_Code ON dbo.Salespersons (SalespersonCode);
GO

CREATE UNIQUE INDEX UX_Salespersons_Email ON dbo.Salespersons (Email) WHERE Email IS NOT NULL;
GO

CREATE TABLE dbo.Items
(
    ItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Items PRIMARY KEY,
    ItemCode NVARCHAR(30) NOT NULL,
    ItemName NVARCHAR(200) NOT NULL,
    PartNumber NVARCHAR(80) NOT NULL,
    ItemType NVARCHAR(30) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    TrackStock BIT NOT NULL CONSTRAINT DF_Items_TrackStock DEFAULT (1),
    IsSerialControlled BIT NOT NULL CONSTRAINT DF_Items_IsSerialControlled DEFAULT (0),
    UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_Items_UnitPrice DEFAULT (0),
    CurrentStock DECIMAL(18,2) NOT NULL CONSTRAINT DF_Items_CurrentStock DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_Items_IsActive DEFAULT (1),
    CONSTRAINT CK_Items_SerialRequiresStock CHECK (IsSerialControlled = 0 OR TrackStock = 1),
    CONSTRAINT CK_Items_NonStockCurrentStock CHECK (TrackStock = 1 OR CurrentStock = 0)
);
GO

CREATE UNIQUE INDEX UX_Items_Code ON dbo.Items (ItemCode);
GO

CREATE UNIQUE INDEX UX_Items_PartNumber ON dbo.Items (PartNumber);
GO

CREATE TABLE dbo.SerialNumbers
(
    SerialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SerialNumbers PRIMARY KEY,
    ItemId INT NOT NULL,
    SerialNo NVARCHAR(120) NOT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_SerialNumbers_Status DEFAULT (N'Available'),
    SupplierId INT NULL,
    CurrentCustomerId INT NULL,
    InvoiceId INT NULL,
    SupplierWarrantyStartDate DATE NULL,
    SupplierWarrantyEndDate DATE NULL,
    CustomerWarrantyStartDate DATE NULL,
    CustomerWarrantyEndDate DATE NULL,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SerialNumbers_CreatedDate DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_SerialNumbers_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items (ItemId) ON DELETE CASCADE,
    CONSTRAINT FK_SerialNumbers_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (SupplierId),
    CONSTRAINT FK_SerialNumbers_Customers FOREIGN KEY (CurrentCustomerId) REFERENCES dbo.Customers (CustomerId)
);
GO

CREATE UNIQUE INDEX UX_SerialNumbers_SerialNo ON dbo.SerialNumbers (SerialNo);
GO
