SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'SRV-0001')
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
        N'SRV-0001',
        N'On-site Installation Service',
        N'SVC-INSTALL-001',
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

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'SRV-0002')
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
        N'SRV-0002',
        N'Network Configuration Service',
        N'SVC-NETCFG-002',
        N'Service',
        N'JOB',
        0,
        0,
        2500.00,
        0.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'SRV-0003')
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
        N'SRV-0003',
        N'Preventive Maintenance Visit',
        N'SVC-PM-003',
        N'Service',
        N'VISIT',
        0,
        0,
        1800.00,
        0.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'SRV-0004')
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
        N'SRV-0004',
        N'User Training Session',
        N'SVC-TRAIN-004',
        N'Service',
        N'SESSION',
        0,
        0,
        3200.00,
        0.00,
        1
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Items WHERE ItemCode = N'SRV-0005')
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
        N'SRV-0005',
        N'Remote Support Service',
        N'SVC-REMOTE-005',
        N'Service',
        N'HOUR',
        0,
        0,
        850.00,
        0.00,
        1
    );
END;
GO

COMMIT TRANSACTION;
GO
