IF COL_LENGTH('dbo.SerialNumbers', 'SupplierId') IS NULL
BEGIN
    ALTER TABLE dbo.SerialNumbers ADD SupplierId INT NULL;
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'SupplierWarrantyStartDate') IS NULL
BEGIN
    ALTER TABLE dbo.SerialNumbers ADD SupplierWarrantyStartDate DATE NULL;
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'SupplierWarrantyEndDate') IS NULL
BEGIN
    ALTER TABLE dbo.SerialNumbers ADD SupplierWarrantyEndDate DATE NULL;
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'CustomerWarrantyStartDate') IS NULL
BEGIN
    ALTER TABLE dbo.SerialNumbers ADD CustomerWarrantyStartDate DATE NULL;
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'CustomerWarrantyEndDate') IS NULL
BEGIN
    ALTER TABLE dbo.SerialNumbers ADD CustomerWarrantyEndDate DATE NULL;
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'WarrantyStartDate') IS NOT NULL
BEGIN
    UPDATE dbo.SerialNumbers
    SET SupplierWarrantyStartDate = COALESCE(SupplierWarrantyStartDate, WarrantyStartDate),
        CustomerWarrantyStartDate = COALESCE(CustomerWarrantyStartDate, WarrantyStartDate);
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'WarrantyEndDate') IS NOT NULL
BEGIN
    UPDATE dbo.SerialNumbers
    SET SupplierWarrantyEndDate = COALESCE(SupplierWarrantyEndDate, WarrantyEndDate),
        CustomerWarrantyEndDate = COALESCE(CustomerWarrantyEndDate, WarrantyEndDate);
END;
GO

UPDATE sn
SET sn.SupplierId = rh.SupplierId
FROM dbo.SerialNumbers sn
INNER JOIN dbo.ReceivingSerials rs
    ON rs.ItemId = sn.ItemId
   AND rs.SerialNo = sn.SerialNo
INNER JOIN dbo.ReceivingDetails rd
    ON rd.ReceivingDetailId = rs.ReceivingDetailId
INNER JOIN dbo.ReceivingHeaders rh
    ON rh.ReceivingId = rd.ReceivingId
WHERE sn.SupplierId IS NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_SerialNumbers_Suppliers'
      AND parent_object_id = OBJECT_ID('dbo.SerialNumbers'))
BEGIN
    ALTER TABLE dbo.SerialNumbers
        ADD CONSTRAINT FK_SerialNumbers_Suppliers
            FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (SupplierId);
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'WarrantyStartDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SerialNumbers DROP COLUMN WarrantyStartDate;
END;
GO

IF COL_LENGTH('dbo.SerialNumbers', 'WarrantyEndDate') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SerialNumbers DROP COLUMN WarrantyEndDate;
END;
GO
