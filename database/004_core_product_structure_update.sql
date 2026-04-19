IF COL_LENGTH('dbo.Items', 'ItemType') IS NULL
BEGIN
    ALTER TABLE dbo.Items ADD ItemType NVARCHAR(30) NOT NULL CONSTRAINT DF_Items_ItemType DEFAULT (N'Product');
END;
GO

IF COL_LENGTH('dbo.Items', 'Unit') IS NULL
BEGIN
    ALTER TABLE dbo.Items ADD Unit NVARCHAR(20) NOT NULL CONSTRAINT DF_Items_Unit DEFAULT (N'EA');
END;
GO

IF COL_LENGTH('dbo.Items', 'CurrentStock') IS NULL
BEGIN
    ALTER TABLE dbo.Items ADD CurrentStock DECIMAL(18,2) NOT NULL CONSTRAINT DF_Items_CurrentStock DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.Items', 'StandardCost') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.Items')
          AND c.name = 'StandardCost')
    BEGIN
        DECLARE @dropStandardCostConstraint NVARCHAR(MAX);
        SELECT @dropStandardCostConstraint =
            N'ALTER TABLE dbo.Items DROP CONSTRAINT ' + QUOTENAME(dc.name)
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.Items')
          AND c.name = 'StandardCost';

        EXEC sp_executesql @dropStandardCostConstraint;
    END;

    ALTER TABLE dbo.Items DROP COLUMN StandardCost;
END;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Items_PartNumber' AND object_id = OBJECT_ID('dbo.Items'))
BEGIN
    DROP INDEX UX_Items_PartNumber ON dbo.Items;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Items_SerialRequiresStock'
      AND parent_object_id = OBJECT_ID('dbo.Items'))
BEGIN
    ALTER TABLE dbo.Items DROP CONSTRAINT CK_Items_SerialRequiresStock;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Items_NonStockCurrentStock'
      AND parent_object_id = OBJECT_ID('dbo.Items'))
BEGIN
    ALTER TABLE dbo.Items DROP CONSTRAINT CK_Items_NonStockCurrentStock;
END;
GO

UPDATE dbo.Items
SET PartNumber = COALESCE(NULLIF(PartNumber, N''), ItemCode),
    ItemType = COALESCE(NULLIF(ItemType, N''), CASE WHEN TrackStock = 1 THEN N'Product' ELSE N'Service' END),
    Unit = COALESCE(NULLIF(Unit, N''), CASE WHEN TrackStock = 1 THEN N'EA' ELSE N'JOB' END),
    CurrentStock = COALESCE(CurrentStock, 0);
GO

ALTER TABLE dbo.Items ALTER COLUMN PartNumber NVARCHAR(80) NOT NULL;
GO

CREATE UNIQUE INDEX UX_Items_PartNumber ON dbo.Items (PartNumber);
GO

ALTER TABLE dbo.Items WITH CHECK
ADD CONSTRAINT CK_Items_SerialRequiresStock CHECK (IsSerialControlled = 0 OR TrackStock = 1);
GO

ALTER TABLE dbo.Items WITH CHECK
ADD CONSTRAINT CK_Items_NonStockCurrentStock CHECK (TrackStock = 1 OR CurrentStock = 0);
GO

IF OBJECT_ID('dbo.SerialNumbers', 'U') IS NULL
BEGIN
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
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SerialNumbers_SerialNo' AND object_id = OBJECT_ID('dbo.SerialNumbers'))
BEGIN
    CREATE UNIQUE INDEX UX_SerialNumbers_SerialNo ON dbo.SerialNumbers (SerialNo);
END;
GO
