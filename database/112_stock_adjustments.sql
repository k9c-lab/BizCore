IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'StockAdjustmentHeaders')
BEGIN
    CREATE TABLE StockAdjustmentHeaders (
        StockAdjustmentId     INT IDENTITY(1,1) NOT NULL,
        AdjustmentNo          NVARCHAR(30) NOT NULL,
        AdjustmentDate        DATE NOT NULL,
        BranchId              INT NOT NULL,
        AdjustmentType        NVARCHAR(30) NOT NULL DEFAULT 'Adjustment',
        Remark                NVARCHAR(500) NULL,
        CreatedByUserId       INT NULL,
        CreatedDate           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_StockAdjustmentHeaders PRIMARY KEY (StockAdjustmentId),
        CONSTRAINT UQ_StockAdjustmentHeaders_AdjustmentNo UNIQUE (AdjustmentNo),
        CONSTRAINT FK_StockAdjustmentHeaders_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId),
        CONSTRAINT FK_StockAdjustmentHeaders_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'StockAdjustmentDetails')
BEGIN
    CREATE TABLE StockAdjustmentDetails (
        StockAdjustmentDetailId INT IDENTITY(1,1) NOT NULL,
        StockAdjustmentId       INT NOT NULL,
        LineNumber               INT NOT NULL,
        ItemId                   INT NOT NULL,
        QtyBefore                DECIMAL(18,2) NOT NULL DEFAULT 0,
        QtyAfter                 DECIMAL(18,2) NOT NULL,
        Remark                   NVARCHAR(500) NULL,
        CONSTRAINT PK_StockAdjustmentDetails PRIMARY KEY (StockAdjustmentDetailId),
        CONSTRAINT FK_StockAdjustmentDetails_Headers FOREIGN KEY (StockAdjustmentId) REFERENCES StockAdjustmentHeaders(StockAdjustmentId) ON DELETE CASCADE,
        CONSTRAINT FK_StockAdjustmentDetails_Items FOREIGN KEY (ItemId) REFERENCES Items(ItemId)
    );
END
