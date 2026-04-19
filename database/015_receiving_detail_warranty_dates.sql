IF COL_LENGTH('dbo.ReceivingDetails', 'SupplierWarrantyStartDate') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingDetails
    ADD SupplierWarrantyStartDate datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.ReceivingDetails', 'SupplierWarrantyEndDate') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingDetails
    ADD SupplierWarrantyEndDate datetime2 NULL;
END;
GO
