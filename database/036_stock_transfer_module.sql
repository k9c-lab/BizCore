IF OBJECT_ID('dbo.StockTransferHeaders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockTransferHeaders
    (
        StockTransferId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransferHeaders PRIMARY KEY,
        TransferNo NVARCHAR(30) NOT NULL,
        TransferDate DATE NOT NULL CONSTRAINT DF_StockTransferHeaders_TransferDate DEFAULT (CONVERT(date, GETDATE())),
        FromBranchId INT NOT NULL,
        ToBranchId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_StockTransferHeaders_Status DEFAULT (N'Draft'),
        Remark NVARCHAR(500) NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        PostedByUserId INT NULL,
        PostedDate DATETIME2 NULL,
        CancelledByUserId INT NULL,
        CancelledDate DATETIME2 NULL,
        CancelReason NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_StockTransferHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CONSTRAINT FK_StockTransferHeaders_FromBranches FOREIGN KEY (FromBranchId) REFERENCES dbo.Branches(BranchId),
        CONSTRAINT FK_StockTransferHeaders_ToBranches FOREIGN KEY (ToBranchId) REFERENCES dbo.Branches(BranchId),
        CONSTRAINT FK_StockTransferHeaders_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_StockTransferHeaders_UpdatedByUsers FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_StockTransferHeaders_PostedByUsers FOREIGN KEY (PostedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_StockTransferHeaders_CancelledByUsers FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_StockTransferHeaders_DifferentBranches CHECK (FromBranchId <> ToBranchId),
        CONSTRAINT CK_StockTransferHeaders_Status CHECK (Status IN (N'Draft', N'Posted', N'Cancelled'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_StockTransferHeaders_TransferNo'
      AND object_id = OBJECT_ID('dbo.StockTransferHeaders')
)
BEGIN
    CREATE UNIQUE INDEX UX_StockTransferHeaders_TransferNo
        ON dbo.StockTransferHeaders(TransferNo);
END;
GO

IF OBJECT_ID('dbo.StockTransferDetails', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockTransferDetails
    (
        StockTransferDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransferDetails PRIMARY KEY,
        StockTransferId INT NOT NULL,
        LineNumber INT NOT NULL,
        ItemId INT NOT NULL,
        Qty DECIMAL(18,2) NOT NULL,
        Remark NVARCHAR(500) NULL,
        CONSTRAINT FK_StockTransferDetails_StockTransferHeaders
            FOREIGN KEY (StockTransferId) REFERENCES dbo.StockTransferHeaders(StockTransferId) ON DELETE CASCADE,
        CONSTRAINT FK_StockTransferDetails_Items
            FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId),
        CONSTRAINT CK_StockTransferDetails_Qty CHECK (Qty > 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_StockTransferDetails_StockTransferId'
      AND object_id = OBJECT_ID('dbo.StockTransferDetails')
)
BEGIN
    CREATE INDEX IX_StockTransferDetails_StockTransferId
        ON dbo.StockTransferDetails(StockTransferId);
END;
GO

IF OBJECT_ID('dbo.StockTransferSerials', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockTransferSerials
    (
        StockTransferSerialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransferSerials PRIMARY KEY,
        StockTransferDetailId INT NOT NULL,
        SerialId INT NOT NULL,
        CONSTRAINT FK_StockTransferSerials_StockTransferDetails
            FOREIGN KEY (StockTransferDetailId) REFERENCES dbo.StockTransferDetails(StockTransferDetailId) ON DELETE CASCADE,
        CONSTRAINT FK_StockTransferSerials_SerialNumbers
            FOREIGN KEY (SerialId) REFERENCES dbo.SerialNumbers(SerialId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_StockTransferSerials_Detail_Serial'
      AND object_id = OBJECT_ID('dbo.StockTransferSerials')
)
BEGIN
    CREATE UNIQUE INDEX UX_StockTransferSerials_Detail_Serial
        ON dbo.StockTransferSerials(StockTransferDetailId, SerialId);
END;
GO
