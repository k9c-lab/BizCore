IF OBJECT_ID('dbo.PurchaseOrderAllocationSources', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrderAllocationSources
    (
        PurchaseOrderAllocationSourceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrderAllocationSources PRIMARY KEY,
        PurchaseOrderAllocationId INT NOT NULL,
        PurchaseRequestDetailId INT NOT NULL,
        SourceQty DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_PurchaseOrderAllocationSources_PurchaseOrderAllocations
            FOREIGN KEY (PurchaseOrderAllocationId) REFERENCES dbo.PurchaseOrderAllocations(PurchaseOrderAllocationId) ON DELETE CASCADE,
        CONSTRAINT FK_PurchaseOrderAllocationSources_PurchaseRequestDetails
            FOREIGN KEY (PurchaseRequestDetailId) REFERENCES dbo.PurchaseRequestDetails(PurchaseRequestDetailId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_PurchaseOrderAllocationSources_Allocation_PRDetail'
      AND object_id = OBJECT_ID('dbo.PurchaseOrderAllocationSources')
)
BEGIN
    CREATE UNIQUE INDEX UX_PurchaseOrderAllocationSources_Allocation_PRDetail
        ON dbo.PurchaseOrderAllocationSources(PurchaseOrderAllocationId, PurchaseRequestDetailId);
END;
GO
