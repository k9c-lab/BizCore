SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, DisplayName, Email, IsActive)
    VALUES (N'admin', N'System Administrator', N'admin@bizcore.local', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'sales.manager')
BEGIN
    INSERT INTO dbo.Users (Username, DisplayName, Email, IsActive)
    VALUES (N'sales.manager', N'Sales Manager', N'sales.manager@bizcore.local', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'inventory.staff')
BEGIN
    INSERT INTO dbo.Users (Username, DisplayName, Email, IsActive)
    VALUES (N'inventory.staff', N'Inventory Staff', N'inventory.staff@bizcore.local', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'CUST-001')
BEGIN
    INSERT INTO dbo.Customers
    (
        CustomerCode,
        CustomerName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    VALUES
    (
        N'CUST-001',
        N'Alpha Trading Co., Ltd.',
        N'0105558010001',
        N'99 Rama 9 Road, Huai Khwang, Bangkok 10310',
        N'02-100-1001',
        N'ap@alphatrading.test',
        150000.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'CUST-002')
BEGIN
    INSERT INTO dbo.Customers
    (
        CustomerCode,
        CustomerName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    VALUES
    (
        N'CUST-002',
        N'Northwind Service Center',
        N'0105558020002',
        N'18 ถนนสุขุมวิท, Khlong Toei, Bangkok 10110',
        N'02-200-2002',
        N'accounting@northwind.test',
        250000.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'CUST-003')
BEGIN
    INSERT INTO dbo.Customers
    (
        CustomerCode,
        CustomerName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    VALUES
    (
        N'CUST-003',
        N'Siam Office Solutions',
        N'0105558030003',
        N'45 Chaeng Watthana Road, Lak Si, Bangkok 10210',
        N'02-300-3003',
        N'purchasing@siamoffice.test',
        90000.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'CUST-004')
BEGIN
    INSERT INTO dbo.Customers
    (
        CustomerCode,
        CustomerName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    VALUES
    (
        N'CUST-004',
        N'Legacy Industrial Supply',
        N'0105558040004',
        N'120 Bangna-Trad Road, Bang Na, Bangkok 10260',
        N'02-400-4004',
        N'finance@legacyindustrial.test',
        50000.00,
        0
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE SupplierCode = N'SUP-001')
BEGIN
    INSERT INTO dbo.Suppliers
    (
        SupplierCode,
        SupplierName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    VALUES
    (
        N'SUP-001',
        N'Tech Components Asia',
        N'0205558011001',
        N'88 Srinakarin Road, Prawet, Bangkok 10250',
        N'02-510-1001',
        N'sales@techcomponents.test',
        300000.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE SupplierCode = N'SUP-002')
BEGIN
    INSERT INTO dbo.Suppliers
    (
        SupplierCode,
        SupplierName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    VALUES
    (
        N'SUP-002',
        N'Prime Packaging Ltd.',
        N'0205558011002',
        N'55 Ekachai Road, Bang Bon, Bangkok 10150',
        N'02-520-2002',
        N'orders@primepack.test',
        120000.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE SupplierCode = N'SUP-003')
BEGIN
    INSERT INTO dbo.Suppliers
    (
        SupplierCode,
        SupplierName,
        TaxId,
        Address,
        PhoneNumber,
        Email,
        CreditLimit,
        IsActive
    )
    VALUES
    (
        N'SUP-003',
        N'Old Town Office Mart',
        N'0205558011003',
        N'16 Tiwanon Road, Mueang Nonthaburi, Nonthaburi 11000',
        N'02-530-3003',
        N'contact@oldtownoffice.test',
        60000.00,
        0
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Salespersons WHERE SalespersonCode = N'SP-001')
BEGIN
    INSERT INTO dbo.Salespersons
    (
        SalespersonCode,
        SalespersonName,
        PhoneNumber,
        Email,
        CommissionRate,
        IsActive
    )
    VALUES
    (
        N'SP-001',
        N'Anan Siripong',
        N'081-111-1001',
        N'anan@bizcore.test',
        3.50,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Salespersons WHERE SalespersonCode = N'SP-002')
BEGIN
    INSERT INTO dbo.Salespersons
    (
        SalespersonCode,
        SalespersonName,
        PhoneNumber,
        Email,
        CommissionRate,
        IsActive
    )
    VALUES
    (
        N'SP-002',
        N'Mali Charoen',
        N'081-111-1002',
        N'mali@bizcore.test',
        4.25,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Salespersons WHERE SalespersonCode = N'SP-003')
BEGIN
    INSERT INTO dbo.Salespersons
    (
        SalespersonCode,
        SalespersonName,
        PhoneNumber,
        Email,
        CommissionRate,
        IsActive
    )
    VALUES
    (
        N'SP-003',
        N'Preecha Wong',
        N'081-111-1003',
        N'preecha@bizcore.test',
        2.75,
        0
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'ITEM-001')
BEGIN
    INSERT INTO dbo.Items
    (
        ItemCode,
        ItemName,
        PartNumber,
        ItemType,
        Unit,
        TrackStock,
        IsSerialControlled,
        UnitPrice,
        CurrentStock,
        IsActive
    )
    VALUES
    (
        N'ITEM-001',
        N'Business Laptop 14-inch',
        N'LT-14-PRO',
        N'Product',
        N'EA',
        1,
        1,
        23900.00,
        8.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'ITEM-002')
BEGIN
    INSERT INTO dbo.Items
    (
        ItemCode,
        ItemName,
        PartNumber,
        ItemType,
        Unit,
        TrackStock,
        IsSerialControlled,
        UnitPrice,
        CurrentStock,
        IsActive
    )
    VALUES
    (
        N'ITEM-002',
        N'24-inch LED Monitor',
        N'MN-24-FHD',
        N'Product',
        N'EA',
        1,
        1,
        4590.00,
        15.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'ITEM-003')
BEGIN
    INSERT INTO dbo.Items
    (
        ItemCode,
        ItemName,
        PartNumber,
        ItemType,
        Unit,
        TrackStock,
        IsSerialControlled,
        UnitPrice,
        CurrentStock,
        IsActive
    )
    VALUES
    (
        N'ITEM-003',
        N'Wireless Keyboard and Mouse Set',
        N'KB-MS-COMBO',
        N'Product',
        N'SET',
        1,
        0,
        990.00,
        25.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'ITEM-004')
BEGIN
    INSERT INTO dbo.Items
    (
        ItemCode,
        ItemName,
        PartNumber,
        ItemType,
        Unit,
        TrackStock,
        IsSerialControlled,
        UnitPrice,
        CurrentStock,
        IsActive
    )
    VALUES
    (
        N'ITEM-004',
        N'On-site Installation Service',
        N'SVC-INSTALL',
        N'Service',
        N'JOB',
        0,
        0,
        1500.00,
        0.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'ITEM-005')
BEGIN
    INSERT INTO dbo.Items
    (
        ItemCode,
        ItemName,
        PartNumber,
        ItemType,
        Unit,
        TrackStock,
        IsSerialControlled,
        UnitPrice,
        CurrentStock,
        IsActive
    )
    VALUES
    (
        N'ITEM-005',
        N'Archived Test Item',
        N'OLD-STOCK-01',
        N'Spare Part',
        N'EA',
        1,
        0,
        180.00,
        3.00,
        0
    );
END;
GO

COMMIT TRANSACTION;
GO
