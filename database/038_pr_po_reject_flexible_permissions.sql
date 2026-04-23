IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions
    (
        PermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY,
        Code NVARCHAR(80) NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Module NVARCHAR(80) NULL
    );

    CREATE UNIQUE INDEX UX_Permissions_Code ON dbo.Permissions(Code);
END;
GO

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RolePermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RolePermissions PRIMARY KEY,
        RoleName NVARCHAR(30) NOT NULL,
        PermissionId INT NOT NULL,
        CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId)
            REFERENCES dbo.Permissions(PermissionId) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX UX_RolePermissions_RoleName_PermissionId
        ON dbo.RolePermissions(RoleName, PermissionId);
END;
GO

IF COL_LENGTH('dbo.PurchaseRequestHeaders', 'RejectedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseRequestHeaders ADD RejectedByUserId INT NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseRequestHeaders', 'RejectedDate') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseRequestHeaders ADD RejectedDate DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseRequestHeaders', 'RejectReason') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseRequestHeaders ADD RejectReason NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'RejectedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD RejectedByUserId INT NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'RejectedDate') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD RejectedDate DATETIME2 NULL;
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'RejectReason') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD RejectReason NVARCHAR(500) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseRequestHeaders_RejectedByUser')
BEGIN
    ALTER TABLE dbo.PurchaseRequestHeaders WITH CHECK
    ADD CONSTRAINT FK_PurchaseRequestHeaders_RejectedByUser
    FOREIGN KEY (RejectedByUserId) REFERENCES dbo.Users(UserId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderHeaders_RejectedByUser')
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders WITH CHECK
    ADD CONSTRAINT FK_PurchaseOrderHeaders_RejectedByUser
    FOREIGN KEY (RejectedByUserId) REFERENCES dbo.Users(UserId);
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
    (N'Sales.Menu', N'Access Sales menu group', N'Menu Access'),
    (N'Sales.Quotations.Menu', N'Access Quotations menu', N'Menu Access'),
    (N'Sales.Invoices.Menu', N'Access Invoices menu', N'Menu Access'),
    (N'Sales.Payments.Menu', N'Access Payments menu', N'Menu Access'),
    (N'Sales.Receipts.Menu', N'Access Receipts menu', N'Menu Access'),
    (N'Purchasing.Menu', N'Access Purchasing menu group', N'Menu Access'),
    (N'Purchasing.PR.Menu', N'Access Purchase Requests menu', N'Menu Access'),
    (N'Purchasing.PO.Menu', N'Access Purchase Orders menu', N'Menu Access'),
    (N'Purchasing.Receiving.Menu', N'Access Receivings menu', N'Menu Access'),
    (N'Inventory.Menu', N'Access Inventory menu group', N'Menu Access'),
    (N'Inventory.StockInquiry.Menu', N'Access Stock Inquiry menu', N'Menu Access'),
    (N'Inventory.SerialInquiry.Menu', N'Access Serial Inquiry menu', N'Menu Access'),
    (N'Inventory.StockLedger.Menu', N'Access Stock Ledger menu', N'Menu Access'),
    (N'Inventory.StockAudit.Menu', N'Access Stock Audit menu', N'Menu Access'),
    (N'Inventory.StockTransfers.Menu', N'Access Stock Transfers menu', N'Menu Access'),
    (N'Inventory.StockIssues.Menu', N'Access Stock Issues menu', N'Menu Access'),
    (N'Warranty.Menu', N'Access Warranty menu group', N'Menu Access'),
    (N'Warranty.CustomerClaims.Menu', N'Access Customer Claims menu', N'Menu Access'),
    (N'Warranty.SupplierClaims.Menu', N'Access Supplier Claims menu', N'Menu Access'),
    (N'Reports.Menu', N'Access Reports menu', N'Menu Access'),
    (N'MasterData.Menu', N'Access Master Data menu group', N'Menu Access'),
    (N'MasterData.Branches.Menu', N'Access Branches menu', N'Menu Access'),
    (N'MasterData.Customers.Menu', N'Access Customers menu', N'Menu Access'),
    (N'MasterData.Suppliers.Menu', N'Access Suppliers menu', N'Menu Access'),
    (N'MasterData.Salespersons.Menu', N'Access Salespersons menu', N'Menu Access'),
    (N'MasterData.Items.Menu', N'Access Items menu', N'Menu Access'),
    (N'MasterData.Users.Menu', N'Access Users menu', N'Menu Access'),
    (N'MasterData.RolePermissions.Menu', N'Access Role Permissions menu', N'Menu Access'),
    (N'PR.View', N'View purchase requests', N'Purchasing'),
    (N'PR.Create', N'Create purchase requests', N'Purchasing'),
    (N'PR.Edit', N'Edit purchase requests', N'Purchasing'),
    (N'PR.Submit', N'Submit purchase requests', N'Purchasing'),
    (N'PR.Approve', N'Approve purchase requests', N'Purchasing'),
    (N'PR.Reject', N'Reject purchase requests', N'Purchasing'),
    (N'PR.Cancel', N'Cancel purchase requests', N'Purchasing'),
    (N'PO.View', N'View purchase orders', N'Purchasing'),
    (N'PO.Create', N'Create purchase orders', N'Purchasing'),
    (N'PO.Edit', N'Edit purchase orders', N'Purchasing'),
    (N'PO.Submit', N'Submit purchase orders', N'Purchasing'),
    (N'PO.Approve', N'Approve purchase orders', N'Purchasing'),
    (N'PO.Reject', N'Reject purchase orders', N'Purchasing'),
    (N'PO.Cancel', N'Cancel purchase orders', N'Purchasing'),
    (N'PO.Receive', N'Receive purchase orders', N'Purchasing'),
    (N'Receiving.View', N'View receiving documents', N'Purchasing'),
    (N'Receiving.Create', N'Create receiving documents', N'Purchasing'),
    (N'Receiving.Edit', N'Edit receiving drafts', N'Purchasing'),
    (N'Receiving.Post', N'Post receiving documents', N'Purchasing'),
    (N'Receiving.Cancel', N'Cancel receiving documents', N'Purchasing'),
    (N'Reports.View', N'View dashboard and reports', N'Reports');

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
    (N'Admin', N'Sales.Menu'),
    (N'Admin', N'Sales.Quotations.Menu'),
    (N'Admin', N'Sales.Invoices.Menu'),
    (N'Admin', N'Sales.Payments.Menu'),
    (N'Admin', N'Sales.Receipts.Menu'),
    (N'Admin', N'Purchasing.Menu'),
    (N'Admin', N'Purchasing.PR.Menu'),
    (N'Admin', N'Purchasing.PO.Menu'),
    (N'Admin', N'Purchasing.Receiving.Menu'),
    (N'Admin', N'Inventory.Menu'),
    (N'Admin', N'Inventory.StockInquiry.Menu'),
    (N'Admin', N'Inventory.SerialInquiry.Menu'),
    (N'Admin', N'Inventory.StockLedger.Menu'),
    (N'Admin', N'Inventory.StockAudit.Menu'),
    (N'Admin', N'Inventory.StockTransfers.Menu'),
    (N'Admin', N'Inventory.StockIssues.Menu'),
    (N'Admin', N'Warranty.Menu'),
    (N'Admin', N'Warranty.CustomerClaims.Menu'),
    (N'Admin', N'Warranty.SupplierClaims.Menu'),
    (N'Admin', N'Reports.Menu'),
    (N'Admin', N'MasterData.Menu'),
    (N'Admin', N'MasterData.Branches.Menu'),
    (N'Admin', N'MasterData.Customers.Menu'),
    (N'Admin', N'MasterData.Suppliers.Menu'),
    (N'Admin', N'MasterData.Salespersons.Menu'),
    (N'Admin', N'MasterData.Items.Menu'),
    (N'Admin', N'MasterData.Users.Menu'),
    (N'Admin', N'MasterData.RolePermissions.Menu'),
    (N'Admin', N'PR.View'),
    (N'Admin', N'PR.Create'),
    (N'Admin', N'PR.Edit'),
    (N'Admin', N'PR.Submit'),
    (N'Admin', N'PR.Approve'),
    (N'Admin', N'PR.Reject'),
    (N'Admin', N'PR.Cancel'),
    (N'Admin', N'PO.View'),
    (N'Admin', N'PO.Create'),
    (N'Admin', N'PO.Edit'),
    (N'Admin', N'PO.Submit'),
    (N'Admin', N'PO.Approve'),
    (N'Admin', N'PO.Reject'),
    (N'Admin', N'PO.Cancel'),
    (N'Admin', N'PO.Receive'),
    (N'Admin', N'Receiving.View'),
    (N'Admin', N'Receiving.Create'),
    (N'Admin', N'Receiving.Edit'),
    (N'Admin', N'Receiving.Post'),
    (N'Admin', N'Receiving.Cancel'),
    (N'Admin', N'Reports.View'),
    (N'CentralAdmin', N'Purchasing.Menu'),
    (N'CentralAdmin', N'Purchasing.PR.Menu'),
    (N'CentralAdmin', N'Purchasing.PO.Menu'),
    (N'CentralAdmin', N'Purchasing.Receiving.Menu'),
    (N'CentralAdmin', N'Reports.Menu'),
    (N'CentralAdmin', N'PR.View'),
    (N'CentralAdmin', N'PR.Approve'),
    (N'CentralAdmin', N'PR.Reject'),
    (N'CentralAdmin', N'PO.View'),
    (N'CentralAdmin', N'PO.Create'),
    (N'CentralAdmin', N'PO.Edit'),
    (N'CentralAdmin', N'PO.Submit'),
    (N'CentralAdmin', N'PO.Cancel'),
    (N'CentralAdmin', N'Receiving.View'),
    (N'CentralAdmin', N'Reports.View'),
    (N'BranchAdmin', N'Sales.Menu'),
    (N'BranchAdmin', N'Sales.Quotations.Menu'),
    (N'BranchAdmin', N'Sales.Invoices.Menu'),
    (N'BranchAdmin', N'Sales.Payments.Menu'),
    (N'BranchAdmin', N'Sales.Receipts.Menu'),
    (N'BranchAdmin', N'Purchasing.Menu'),
    (N'BranchAdmin', N'Purchasing.PR.Menu'),
    (N'BranchAdmin', N'Purchasing.PO.Menu'),
    (N'BranchAdmin', N'Purchasing.Receiving.Menu'),
    (N'BranchAdmin', N'Inventory.Menu'),
    (N'BranchAdmin', N'Inventory.StockInquiry.Menu'),
    (N'BranchAdmin', N'Inventory.SerialInquiry.Menu'),
    (N'BranchAdmin', N'Inventory.StockLedger.Menu'),
    (N'BranchAdmin', N'Inventory.StockAudit.Menu'),
    (N'BranchAdmin', N'Inventory.StockTransfers.Menu'),
    (N'BranchAdmin', N'Inventory.StockIssues.Menu'),
    (N'BranchAdmin', N'Warranty.Menu'),
    (N'BranchAdmin', N'Warranty.CustomerClaims.Menu'),
    (N'BranchAdmin', N'Warranty.SupplierClaims.Menu'),
    (N'BranchAdmin', N'Reports.Menu'),
    (N'BranchAdmin', N'PR.View'),
    (N'BranchAdmin', N'PR.Create'),
    (N'BranchAdmin', N'PR.Edit'),
    (N'BranchAdmin', N'PR.Submit'),
    (N'BranchAdmin', N'PR.Cancel'),
    (N'BranchAdmin', N'PO.View'),
    (N'BranchAdmin', N'Receiving.View'),
    (N'BranchAdmin', N'Receiving.Create'),
    (N'BranchAdmin', N'Receiving.Edit'),
    (N'BranchAdmin', N'Receiving.Post'),
    (N'BranchAdmin', N'Receiving.Cancel'),
    (N'BranchAdmin', N'Reports.View'),
    (N'Sales', N'Sales.Menu'),
    (N'Sales', N'Sales.Quotations.Menu'),
    (N'Sales', N'Sales.Invoices.Menu'),
    (N'Sales', N'Sales.Payments.Menu'),
    (N'Sales', N'Sales.Receipts.Menu'),
    (N'Sales', N'Reports.Menu'),
    (N'Sales', N'Reports.View'),
    (N'Warehouse', N'Purchasing.Menu'),
    (N'Warehouse', N'Purchasing.PR.Menu'),
    (N'Warehouse', N'Purchasing.PO.Menu'),
    (N'Warehouse', N'Purchasing.Receiving.Menu'),
    (N'Warehouse', N'Inventory.Menu'),
    (N'Warehouse', N'Inventory.StockInquiry.Menu'),
    (N'Warehouse', N'Inventory.SerialInquiry.Menu'),
    (N'Warehouse', N'Inventory.StockLedger.Menu'),
    (N'Warehouse', N'Inventory.StockAudit.Menu'),
    (N'Warehouse', N'Inventory.StockTransfers.Menu'),
    (N'Warehouse', N'Inventory.StockIssues.Menu'),
    (N'Warehouse', N'Warranty.Menu'),
    (N'Warehouse', N'Warranty.CustomerClaims.Menu'),
    (N'Warehouse', N'Warranty.SupplierClaims.Menu'),
    (N'Warehouse', N'Reports.Menu'),
    (N'Warehouse', N'PR.View'),
    (N'Warehouse', N'PR.Create'),
    (N'Warehouse', N'PR.Edit'),
    (N'Warehouse', N'PR.Submit'),
    (N'Warehouse', N'PR.Cancel'),
    (N'Warehouse', N'PO.View'),
    (N'Warehouse', N'PO.Receive'),
    (N'Warehouse', N'Receiving.View'),
    (N'Warehouse', N'Receiving.Create'),
    (N'Warehouse', N'Receiving.Edit'),
    (N'Warehouse', N'Receiving.Post'),
    (N'Warehouse', N'Receiving.Cancel'),
    (N'Warehouse', N'Reports.View'),
    (N'Executive', N'Purchasing.Menu'),
    (N'Executive', N'Purchasing.PR.Menu'),
    (N'Executive', N'Purchasing.PO.Menu'),
    (N'Executive', N'Purchasing.Receiving.Menu'),
    (N'Executive', N'Sales.Menu'),
    (N'Executive', N'Sales.Quotations.Menu'),
    (N'Executive', N'Sales.Invoices.Menu'),
    (N'Executive', N'Sales.Payments.Menu'),
    (N'Executive', N'Sales.Receipts.Menu'),
    (N'Executive', N'Reports.Menu'),
    (N'Executive', N'PR.View'),
    (N'Executive', N'PO.View'),
    (N'Executive', N'PO.Approve'),
    (N'Executive', N'PO.Reject'),
    (N'Executive', N'Receiving.View'),
    (N'Executive', N'Reports.View');

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
