IF OBJECT_ID(N'dbo.PurchaseOrderAllocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrderAllocations
    (
        PurchaseOrderAllocationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrderAllocations PRIMARY KEY,
        PurchaseOrderDetailId INT NOT NULL,
        BranchId INT NOT NULL,
        AllocatedQty DECIMAL(18,2) NOT NULL,
        ReceivedQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseOrderAllocations_ReceivedQty DEFAULT (0),
        CONSTRAINT FK_PurchaseOrderAllocations_PurchaseOrderDetails FOREIGN KEY (PurchaseOrderDetailId)
            REFERENCES dbo.PurchaseOrderDetails(PurchaseOrderDetailId) ON DELETE CASCADE,
        CONSTRAINT FK_PurchaseOrderAllocations_Branches FOREIGN KEY (BranchId)
            REFERENCES dbo.Branches(BranchId)
    );

    CREATE UNIQUE INDEX UX_PurchaseOrderAllocations_Detail_Branch
        ON dbo.PurchaseOrderAllocations(PurchaseOrderDetailId, BranchId);
END;
GO

IF COL_LENGTH('dbo.ReceivingDetails', 'PurchaseOrderAllocationId') IS NULL
BEGIN
    ALTER TABLE dbo.ReceivingDetails ADD PurchaseOrderAllocationId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_ReceivingDetails_PurchaseOrderAllocations'
)
BEGIN
    ALTER TABLE dbo.ReceivingDetails
    ADD CONSTRAINT FK_ReceivingDetails_PurchaseOrderAllocations
        FOREIGN KEY (PurchaseOrderAllocationId) REFERENCES dbo.PurchaseOrderAllocations(PurchaseOrderAllocationId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ReceivingDetails_PurchaseOrderAllocationId'
      AND object_id = OBJECT_ID(N'dbo.ReceivingDetails')
)
BEGIN
    CREATE INDEX IX_ReceivingDetails_PurchaseOrderAllocationId
        ON dbo.ReceivingDetails(PurchaseOrderAllocationId);
END;
GO

INSERT INTO dbo.PurchaseOrderAllocations (PurchaseOrderDetailId, BranchId, AllocatedQty, ReceivedQty)
SELECT
    d.PurchaseOrderDetailId,
    h.BranchId,
    d.Qty,
    d.ReceivedQty
FROM dbo.PurchaseOrderDetails d
INNER JOIN dbo.PurchaseOrderHeaders h ON h.PurchaseOrderId = d.PurchaseOrderId
WHERE h.BranchId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.PurchaseOrderAllocations a
      WHERE a.PurchaseOrderDetailId = d.PurchaseOrderDetailId
  );
GO

UPDATE rd
SET PurchaseOrderAllocationId = a.PurchaseOrderAllocationId
FROM dbo.ReceivingDetails rd
INNER JOIN dbo.ReceivingHeaders rh ON rh.ReceivingId = rd.ReceivingId
INNER JOIN dbo.PurchaseOrderAllocations a
    ON a.PurchaseOrderDetailId = rd.PurchaseOrderDetailId
   AND a.BranchId = rh.BranchId
WHERE rd.PurchaseOrderAllocationId IS NULL;
GO
