-- Branch stock foundation.
-- Safe to run multiple times after 026_branch_foundation.sql.

IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.SerialNumbers', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SerialNumbers', 'BranchId') IS NULL
        ALTER TABLE dbo.SerialNumbers ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SerialNumbers_Branches_BranchId')
        ALTER TABLE dbo.SerialNumbers ADD CONSTRAINT FK_SerialNumbers_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SerialNumbers_BranchId' AND object_id = OBJECT_ID(N'dbo.SerialNumbers'))
        CREATE INDEX IX_SerialNumbers_BranchId ON dbo.SerialNumbers(BranchId);

    DECLARE @MainBranchId INT = (SELECT TOP 1 BranchId FROM dbo.Branches WHERE BranchCode = N'MAIN');

    IF @MainBranchId IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE dbo.SerialNumbers SET BranchId = @BranchId WHERE BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;
END;
GO

IF OBJECT_ID(N'dbo.StockBalances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockBalances
    (
        StockBalanceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockBalances PRIMARY KEY,
        ItemId INT NOT NULL,
        BranchId INT NOT NULL,
        QtyOnHand DECIMAL(18,2) NOT NULL CONSTRAINT DF_StockBalances_QtyOnHand DEFAULT (0),
        CONSTRAINT FK_StockBalances_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId),
        CONSTRAINT FK_StockBalances_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId)
    );

    CREATE UNIQUE INDEX UX_StockBalances_ItemId_BranchId ON dbo.StockBalances(ItemId, BranchId);
END;
GO

IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovements
    (
        StockMovementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovements PRIMARY KEY,
        MovementDate DATETIME2 NOT NULL,
        MovementType NVARCHAR(30) NOT NULL,
        ReferenceType NVARCHAR(30) NULL,
        ReferenceId INT NULL,
        ItemId INT NOT NULL,
        SerialId INT NULL,
        FromBranchId INT NULL,
        ToBranchId INT NULL,
        Qty DECIMAL(18,2) NOT NULL,
        Remark NVARCHAR(500) NULL,
        CreatedByUserId INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_StockMovements_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StockMovements_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId),
        CONSTRAINT FK_StockMovements_SerialNumbers FOREIGN KEY (SerialId) REFERENCES dbo.SerialNumbers(SerialId),
        CONSTRAINT FK_StockMovements_FromBranches FOREIGN KEY (FromBranchId) REFERENCES dbo.Branches(BranchId),
        CONSTRAINT FK_StockMovements_ToBranches FOREIGN KEY (ToBranchId) REFERENCES dbo.Branches(BranchId),
        CONSTRAINT FK_StockMovements_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId)
    );

    CREATE INDEX IX_StockMovements_MovementDate ON dbo.StockMovements(MovementDate);
    CREATE INDEX IX_StockMovements_ItemId ON dbo.StockMovements(ItemId);
    CREATE INDEX IX_StockMovements_SerialId ON dbo.StockMovements(SerialId);
END;
GO

IF OBJECT_ID(N'dbo.PurchaseOrderHeaders', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'BranchId') IS NULL
        ALTER TABLE dbo.PurchaseOrderHeaders ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderHeaders_Branches_BranchId')
        ALTER TABLE dbo.PurchaseOrderHeaders ADD CONSTRAINT FK_PurchaseOrderHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrderHeaders_BranchId' AND object_id = OBJECT_ID(N'dbo.PurchaseOrderHeaders'))
        CREATE INDEX IX_PurchaseOrderHeaders_BranchId ON dbo.PurchaseOrderHeaders(BranchId);
END;
GO

IF OBJECT_ID(N'dbo.ReceivingHeaders', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ReceivingHeaders', 'BranchId') IS NULL
        ALTER TABLE dbo.ReceivingHeaders ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceivingHeaders_Branches_BranchId')
        ALTER TABLE dbo.ReceivingHeaders ADD CONSTRAINT FK_ReceivingHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceivingHeaders_BranchId' AND object_id = OBJECT_ID(N'dbo.ReceivingHeaders'))
        CREATE INDEX IX_ReceivingHeaders_BranchId ON dbo.ReceivingHeaders(BranchId);
END;
GO

DECLARE @MainBranchId INT = (SELECT TOP 1 BranchId FROM dbo.Branches WHERE BranchCode = N'MAIN');

IF @MainBranchId IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.PurchaseOrderHeaders', N'U') IS NOT NULL
        EXEC sp_executesql
            N'UPDATE dbo.PurchaseOrderHeaders SET BranchId = @BranchId WHERE BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;

    IF OBJECT_ID(N'dbo.ReceivingHeaders', N'U') IS NOT NULL
        EXEC sp_executesql
            N'UPDATE dbo.ReceivingHeaders SET BranchId = @BranchId WHERE BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;

    INSERT INTO dbo.StockBalances (ItemId, BranchId, QtyOnHand)
    SELECT i.ItemId, @MainBranchId, i.CurrentStock
    FROM dbo.Items i
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.StockBalances b
        WHERE b.ItemId = i.ItemId
          AND b.BranchId = @MainBranchId
    );

    INSERT INTO dbo.StockMovements
        (MovementDate, MovementType, ReferenceType, ReferenceId, ItemId, SerialId, FromBranchId, ToBranchId, Qty, Remark, CreatedByUserId)
    SELECT
        SYSUTCDATETIME(),
        N'OpeningBalance',
        N'Migration',
        NULL,
        i.ItemId,
        NULL,
        NULL,
        @MainBranchId,
        i.CurrentStock,
        N'Opening balance from Items.CurrentStock during branch stock foundation.',
        NULL
    FROM dbo.Items i
    WHERE i.CurrentStock <> 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.StockMovements m
          WHERE m.ItemId = i.ItemId
            AND m.ToBranchId = @MainBranchId
            AND m.MovementType = N'OpeningBalance'
            AND m.ReferenceType = N'Migration'
      );
END;
GO
