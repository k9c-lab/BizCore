-- Branch isolation for remaining operational documents.
-- Run after 026, 027, 028.

DECLARE @MainBranchId INT = NULL;

IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    SELECT TOP 1 @MainBranchId = BranchId
    FROM dbo.Branches
    WHERE BranchCode = N'MAIN'
    ORDER BY BranchId;
END;

IF OBJECT_ID(N'dbo.QuotationHeaders', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.QuotationHeaders', 'BranchId') IS NULL
        ALTER TABLE dbo.QuotationHeaders ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_QuotationHeaders_Branches_BranchId')
        ALTER TABLE dbo.QuotationHeaders ADD CONSTRAINT FK_QuotationHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QuotationHeaders_BranchId' AND object_id = OBJECT_ID(N'dbo.QuotationHeaders'))
        CREATE INDEX IX_QuotationHeaders_BranchId ON dbo.QuotationHeaders(BranchId);
END;
GO

IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PaymentHeaders', 'BranchId') IS NULL
        ALTER TABLE dbo.PaymentHeaders ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentHeaders_Branches_BranchId')
        ALTER TABLE dbo.PaymentHeaders ADD CONSTRAINT FK_PaymentHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentHeaders_BranchId' AND object_id = OBJECT_ID(N'dbo.PaymentHeaders'))
        CREATE INDEX IX_PaymentHeaders_BranchId ON dbo.PaymentHeaders(BranchId);
END;
GO

IF OBJECT_ID(N'dbo.ReceiptHeaders', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ReceiptHeaders', 'BranchId') IS NULL
        ALTER TABLE dbo.ReceiptHeaders ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceiptHeaders_Branches_BranchId')
        ALTER TABLE dbo.ReceiptHeaders ADD CONSTRAINT FK_ReceiptHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptHeaders_BranchId' AND object_id = OBJECT_ID(N'dbo.ReceiptHeaders'))
        CREATE INDEX IX_ReceiptHeaders_BranchId ON dbo.ReceiptHeaders(BranchId);
END;
GO

IF OBJECT_ID(N'dbo.CustomerClaimHeaders', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.CustomerClaimHeaders', 'BranchId') IS NULL
        ALTER TABLE dbo.CustomerClaimHeaders ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CustomerClaimHeaders_Branches_BranchId')
        ALTER TABLE dbo.CustomerClaimHeaders ADD CONSTRAINT FK_CustomerClaimHeaders_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CustomerClaimHeaders_BranchId' AND object_id = OBJECT_ID(N'dbo.CustomerClaimHeaders'))
        CREATE INDEX IX_CustomerClaimHeaders_BranchId ON dbo.CustomerClaimHeaders(BranchId);
END;
GO

IF OBJECT_ID(N'dbo.SerialClaimLogs', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SerialClaimLogs', 'BranchId') IS NULL
        ALTER TABLE dbo.SerialClaimLogs ADD BranchId INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SerialClaimLogs_Branches_BranchId')
        ALTER TABLE dbo.SerialClaimLogs ADD CONSTRAINT FK_SerialClaimLogs_Branches_BranchId FOREIGN KEY (BranchId) REFERENCES dbo.Branches(BranchId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SerialClaimLogs_BranchId' AND object_id = OBJECT_ID(N'dbo.SerialClaimLogs'))
        CREATE INDEX IX_SerialClaimLogs_BranchId ON dbo.SerialClaimLogs(BranchId);
END;
GO

DECLARE @MainBranchId INT = (SELECT TOP 1 BranchId FROM dbo.Branches WHERE BranchCode = N'MAIN' ORDER BY BranchId);

IF @MainBranchId IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.QuotationHeaders', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE q
              SET BranchId = COALESCE(i.BranchId, @BranchId)
              FROM dbo.QuotationHeaders q
              OUTER APPLY
              (
                  SELECT TOP 1 BranchId
                  FROM dbo.InvoiceHeaders i
                  WHERE i.QuotationId = q.QuotationHeaderId
                  ORDER BY i.InvoiceId DESC
              ) i
              WHERE q.BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;

    IF OBJECT_ID(N'dbo.PaymentHeaders', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE p
              SET BranchId = COALESCE(i.BranchId, @BranchId)
              FROM dbo.PaymentHeaders p
              OUTER APPLY
              (
                  SELECT TOP 1 ih.BranchId
                  FROM dbo.PaymentAllocations pa
                  INNER JOIN dbo.InvoiceHeaders ih ON ih.InvoiceId = pa.InvoiceId
                  WHERE pa.PaymentId = p.PaymentId
                  ORDER BY pa.PaymentAllocationId
              ) i
              WHERE p.BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;

    IF OBJECT_ID(N'dbo.ReceiptHeaders', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE r
              SET BranchId = COALESCE(p.BranchId, @BranchId)
              FROM dbo.ReceiptHeaders r
              LEFT JOIN dbo.PaymentHeaders p ON p.PaymentId = r.PaymentId
              WHERE r.BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;

    IF OBJECT_ID(N'dbo.CustomerClaimHeaders', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE c
              SET BranchId = COALESCE(s.BranchId, i.BranchId, @BranchId)
              FROM dbo.CustomerClaimHeaders c
              LEFT JOIN dbo.InvoiceHeaders i ON i.InvoiceId = c.InvoiceId
              OUTER APPLY
              (
                  SELECT TOP 1 sn.BranchId
                  FROM dbo.CustomerClaimDetails d
                  INNER JOIN dbo.SerialNumbers sn ON sn.SerialId = d.SerialId
                  WHERE d.CustomerClaimId = c.CustomerClaimId
                  ORDER BY d.CustomerClaimDetailId
              ) s
              WHERE c.BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;

    IF OBJECT_ID(N'dbo.SerialClaimLogs', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql
            N'UPDATE scl
              SET BranchId = COALESCE(sn.BranchId, cc.BranchId, @BranchId)
              FROM dbo.SerialClaimLogs scl
              LEFT JOIN dbo.SerialNumbers sn ON sn.SerialId = scl.SerialId
              LEFT JOIN dbo.CustomerClaimHeaders cc ON cc.CustomerClaimId = scl.CustomerClaimId
              WHERE scl.BranchId IS NULL;',
            N'@BranchId INT',
            @BranchId = @MainBranchId;
    END;
END;
GO
