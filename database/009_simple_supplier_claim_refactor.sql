IF OBJECT_ID(N'dbo.SerialClaimLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SerialClaimLogs
    (
        SerialClaimLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SerialClaimLogs PRIMARY KEY,
        SerialId INT NOT NULL,
        SupplierId INT NOT NULL,
        ClaimDate DATETIME2 NOT NULL,
        ProblemDescription NVARCHAR(1000) NULL,
        ClaimStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_SerialClaimLogs_ClaimStatus DEFAULT (N'Open'),
        Remark NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SerialClaimLogs_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CONSTRAINT FK_SerialClaimLogs_SerialNumbers FOREIGN KEY (SerialId) REFERENCES dbo.SerialNumbers (SerialId),
        CONSTRAINT FK_SerialClaimLogs_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (SupplierId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SerialClaimLogs_SerialId' AND object_id = OBJECT_ID(N'dbo.SerialClaimLogs'))
BEGIN
    CREATE INDEX IX_SerialClaimLogs_SerialId ON dbo.SerialClaimLogs (SerialId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SerialClaimLogs_SupplierId' AND object_id = OBJECT_ID(N'dbo.SerialClaimLogs'))
BEGIN
    CREATE INDEX IX_SerialClaimLogs_SupplierId ON dbo.SerialClaimLogs (SupplierId);
END;
GO

PRINT N'SerialClaimLogs is ready. Old SupplierClaimHeaders and SupplierClaimDetails tables are left untouched but are no longer used by the app.';
GO
