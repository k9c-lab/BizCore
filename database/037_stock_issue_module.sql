IF OBJECT_ID('dbo.StockIssueHeaders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockIssueHeaders
    (
        StockIssueId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockIssueHeaders PRIMARY KEY,
        IssueNo NVARCHAR(30) NOT NULL,
        IssueDate DATE NOT NULL CONSTRAINT DF_StockIssueHeaders_IssueDate DEFAULT (CONVERT(date, GETDATE())),
        BranchId INT NOT NULL,
        IssueType NVARCHAR(30) NOT NULL CONSTRAINT DF_StockIssueHeaders_IssueType DEFAULT (N'InternalUse'),
        Purpose NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_StockIssueHeaders_Status DEFAULT (N'Draft'),
        Remark NVARCHAR(500) NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        PostedByUserId INT NULL,
        PostedDate DATETIME2 NULL,
        CancelledByUserId INT NULL,
        CancelledDate DATETIME2 NULL,
        CancelReason NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_StockIssueHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CONSTRAINT FK_StockIssueHeaders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId),
        CONSTRAINT FK_StockIssueHeaders_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_StockIssueHeaders_UpdatedByUsers FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_StockIssueHeaders_PostedByUsers FOREIGN KEY (PostedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_StockIssueHeaders_CancelledByUsers FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_StockIssueHeaders_Status CHECK (Status IN (N'Draft', N'Posted', N'Cancelled')),
        CONSTRAINT CK_StockIssueHeaders_IssueType CHECK (IssueType IN (N'InternalUse', N'Damaged', N'Demo', N'Adjustment', N'Other'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_StockIssueHeaders_IssueNo'
      AND object_id = OBJECT_ID('dbo.StockIssueHeaders')
)
BEGIN
    CREATE UNIQUE INDEX UX_StockIssueHeaders_IssueNo
        ON dbo.StockIssueHeaders(IssueNo);
END;
GO

IF OBJECT_ID('dbo.StockIssueDetails', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockIssueDetails
    (
        StockIssueDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockIssueDetails PRIMARY KEY,
        StockIssueId INT NOT NULL,
        LineNumber INT NOT NULL,
        ItemId INT NOT NULL,
        Qty DECIMAL(18,2) NOT NULL,
        Remark NVARCHAR(500) NULL,
        CONSTRAINT FK_StockIssueDetails_StockIssueHeaders
            FOREIGN KEY (StockIssueId) REFERENCES dbo.StockIssueHeaders(StockIssueId) ON DELETE CASCADE,
        CONSTRAINT FK_StockIssueDetails_Items
            FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId),
        CONSTRAINT CK_StockIssueDetails_Qty CHECK (Qty > 0)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_StockIssueDetails_StockIssueId'
      AND object_id = OBJECT_ID('dbo.StockIssueDetails')
)
BEGIN
    CREATE INDEX IX_StockIssueDetails_StockIssueId
        ON dbo.StockIssueDetails(StockIssueId);
END;
GO

IF OBJECT_ID('dbo.StockIssueSerials', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockIssueSerials
    (
        StockIssueSerialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockIssueSerials PRIMARY KEY,
        StockIssueDetailId INT NOT NULL,
        SerialId INT NOT NULL,
        CONSTRAINT FK_StockIssueSerials_StockIssueDetails
            FOREIGN KEY (StockIssueDetailId) REFERENCES dbo.StockIssueDetails(StockIssueDetailId) ON DELETE CASCADE,
        CONSTRAINT FK_StockIssueSerials_SerialNumbers
            FOREIGN KEY (SerialId) REFERENCES dbo.SerialNumbers(SerialId)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_StockIssueSerials_Detail_Serial'
      AND object_id = OBJECT_ID('dbo.StockIssueSerials')
)
BEGIN
    CREATE UNIQUE INDEX UX_StockIssueSerials_Detail_Serial
        ON dbo.StockIssueSerials(StockIssueDetailId, SerialId);
END;
GO
