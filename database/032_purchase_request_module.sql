IF OBJECT_ID(N'dbo.PurchaseRequestHeaders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseRequestHeaders
    (
        PurchaseRequestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseRequestHeaders PRIMARY KEY,
        PRNo NVARCHAR(30) NOT NULL,
        RequestDate DATE NOT NULL,
        RequiredDate DATE NULL,
        BranchId INT NULL,
        Purpose NVARCHAR(1000) NULL,
        Remark NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PurchaseRequestHeaders_Status DEFAULT N'Draft',
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        SubmittedByUserId INT NULL,
        SubmittedDate DATETIME2 NULL,
        ApprovedByUserId INT NULL,
        ApprovedDate DATETIME2 NULL,
        CancelledByUserId INT NULL,
        CancelledDate DATETIME2 NULL,
        CancelReason NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_PurchaseRequestHeaders_CreatedDate DEFAULT SYSUTCDATETIME(),
        UpdatedDate DATETIME2 NULL,
        CONSTRAINT FK_PurchaseRequestHeaders_Branches FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId),
        CONSTRAINT FK_PurchaseRequestHeaders_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_PurchaseRequestHeaders_UpdatedByUser FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_PurchaseRequestHeaders_SubmittedByUser FOREIGN KEY (SubmittedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_PurchaseRequestHeaders_ApprovedByUser FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_PurchaseRequestHeaders_CancelledByUser FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId)
    );

    CREATE UNIQUE INDEX UX_PurchaseRequestHeaders_PRNo ON dbo.PurchaseRequestHeaders(PRNo);
END;
GO

IF OBJECT_ID(N'dbo.PurchaseRequestDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseRequestDetails
    (
        PurchaseRequestDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseRequestDetails PRIMARY KEY,
        PurchaseRequestId INT NOT NULL,
        LineNumber INT NOT NULL,
        ItemId INT NOT NULL,
        RequestedQty DECIMAL(18,2) NOT NULL,
        Remark NVARCHAR(500) NULL,
        CONSTRAINT FK_PurchaseRequestDetails_PurchaseRequestHeaders FOREIGN KEY (PurchaseRequestId)
            REFERENCES dbo.PurchaseRequestHeaders(PurchaseRequestId) ON DELETE CASCADE,
        CONSTRAINT FK_PurchaseRequestDetails_Items FOREIGN KEY (ItemId)
            REFERENCES dbo.Items(ItemId)
    );
END;
GO

IF COL_LENGTH('dbo.PurchaseOrderHeaders', 'PurchaseRequestId') IS NULL
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders ADD PurchaseRequestId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_PurchaseOrderHeaders_PurchaseRequestHeaders'
)
BEGIN
    ALTER TABLE dbo.PurchaseOrderHeaders
    ADD CONSTRAINT FK_PurchaseOrderHeaders_PurchaseRequestHeaders
        FOREIGN KEY (PurchaseRequestId) REFERENCES dbo.PurchaseRequestHeaders(PurchaseRequestId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PurchaseOrderHeaders_PurchaseRequestId'
      AND object_id = OBJECT_ID(N'dbo.PurchaseOrderHeaders')
)
BEGIN
    CREATE INDEX IX_PurchaseOrderHeaders_PurchaseRequestId ON dbo.PurchaseOrderHeaders(PurchaseRequestId);
END;
GO
