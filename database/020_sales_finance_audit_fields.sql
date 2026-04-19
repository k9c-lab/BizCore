-- Adds document audit fields for Quotation, Invoice, Payment, and Receipt modules.
-- Safe to run multiple times.

IF COL_LENGTH('dbo.QuotationHeaders', 'CreatedByUserId') IS NULL
    ALTER TABLE dbo.QuotationHeaders ADD CreatedByUserId INT NULL;
IF COL_LENGTH('dbo.QuotationHeaders', 'UpdatedByUserId') IS NULL
    ALTER TABLE dbo.QuotationHeaders ADD UpdatedByUserId INT NULL;
IF COL_LENGTH('dbo.QuotationHeaders', 'ApprovedByUserId') IS NULL
    ALTER TABLE dbo.QuotationHeaders ADD ApprovedByUserId INT NULL;
IF COL_LENGTH('dbo.QuotationHeaders', 'ApprovedDate') IS NULL
    ALTER TABLE dbo.QuotationHeaders ADD ApprovedDate DATETIME2 NULL;
IF COL_LENGTH('dbo.QuotationHeaders', 'ConvertedByUserId') IS NULL
    ALTER TABLE dbo.QuotationHeaders ADD ConvertedByUserId INT NULL;
IF COL_LENGTH('dbo.QuotationHeaders', 'ConvertedDate') IS NULL
    ALTER TABLE dbo.QuotationHeaders ADD ConvertedDate DATETIME2 NULL;
GO

IF COL_LENGTH('dbo.InvoiceHeaders', 'CreatedByUserId') IS NULL
    ALTER TABLE dbo.InvoiceHeaders ADD CreatedByUserId INT NULL;
IF COL_LENGTH('dbo.InvoiceHeaders', 'UpdatedByUserId') IS NULL
    ALTER TABLE dbo.InvoiceHeaders ADD UpdatedByUserId INT NULL;
IF COL_LENGTH('dbo.InvoiceHeaders', 'IssuedByUserId') IS NULL
    ALTER TABLE dbo.InvoiceHeaders ADD IssuedByUserId INT NULL;
IF COL_LENGTH('dbo.InvoiceHeaders', 'IssuedDate') IS NULL
    ALTER TABLE dbo.InvoiceHeaders ADD IssuedDate DATETIME2 NULL;
IF COL_LENGTH('dbo.InvoiceHeaders', 'CancelledByUserId') IS NULL
    ALTER TABLE dbo.InvoiceHeaders ADD CancelledByUserId INT NULL;
IF COL_LENGTH('dbo.InvoiceHeaders', 'CancelledDate') IS NULL
    ALTER TABLE dbo.InvoiceHeaders ADD CancelledDate DATETIME2 NULL;
IF COL_LENGTH('dbo.InvoiceHeaders', 'CancelReason') IS NULL
    ALTER TABLE dbo.InvoiceHeaders ADD CancelReason NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.PaymentHeaders', 'CreatedByUserId') IS NULL
    ALTER TABLE dbo.PaymentHeaders ADD CreatedByUserId INT NULL;
IF COL_LENGTH('dbo.PaymentHeaders', 'PostedByUserId') IS NULL
    ALTER TABLE dbo.PaymentHeaders ADD PostedByUserId INT NULL;
IF COL_LENGTH('dbo.PaymentHeaders', 'PostedDate') IS NULL
    ALTER TABLE dbo.PaymentHeaders ADD PostedDate DATETIME2 NULL;
IF COL_LENGTH('dbo.PaymentHeaders', 'CancelledByUserId') IS NULL
    ALTER TABLE dbo.PaymentHeaders ADD CancelledByUserId INT NULL;
IF COL_LENGTH('dbo.PaymentHeaders', 'CancelledDate') IS NULL
    ALTER TABLE dbo.PaymentHeaders ADD CancelledDate DATETIME2 NULL;
IF COL_LENGTH('dbo.PaymentHeaders', 'CancelReason') IS NULL
    ALTER TABLE dbo.PaymentHeaders ADD CancelReason NVARCHAR(500) NULL;
GO

IF COL_LENGTH('dbo.ReceiptHeaders', 'CreatedByUserId') IS NULL
    ALTER TABLE dbo.ReceiptHeaders ADD CreatedByUserId INT NULL;
IF COL_LENGTH('dbo.ReceiptHeaders', 'IssuedByUserId') IS NULL
    ALTER TABLE dbo.ReceiptHeaders ADD IssuedByUserId INT NULL;
IF COL_LENGTH('dbo.ReceiptHeaders', 'IssuedDate') IS NULL
    ALTER TABLE dbo.ReceiptHeaders ADD IssuedDate DATETIME2 NULL;
IF COL_LENGTH('dbo.ReceiptHeaders', 'CancelledByUserId') IS NULL
    ALTER TABLE dbo.ReceiptHeaders ADD CancelledByUserId INT NULL;
IF COL_LENGTH('dbo.ReceiptHeaders', 'CancelledDate') IS NULL
    ALTER TABLE dbo.ReceiptHeaders ADD CancelledDate DATETIME2 NULL;
IF COL_LENGTH('dbo.ReceiptHeaders', 'CancelReason') IS NULL
    ALTER TABLE dbo.ReceiptHeaders ADD CancelReason NVARCHAR(500) NULL;
GO

DECLARE @DefaultUserId INT;

SELECT TOP (1) @DefaultUserId = UserId
FROM dbo.Users
WHERE Username = N'admin'
ORDER BY UserId;

IF @DefaultUserId IS NULL
BEGIN
    SELECT TOP (1) @DefaultUserId = UserId
    FROM dbo.Users
    WHERE IsActive = 1
    ORDER BY UserId;
END;

IF @DefaultUserId IS NOT NULL
BEGIN
    UPDATE dbo.QuotationHeaders
    SET CreatedByUserId = COALESCE(CreatedByUserId, @DefaultUserId),
        UpdatedByUserId = CASE WHEN UpdatedDate IS NOT NULL THEN COALESCE(UpdatedByUserId, @DefaultUserId) ELSE UpdatedByUserId END,
        ApprovedByUserId = CASE WHEN Status IN (N'Approved', N'Converted') THEN COALESCE(ApprovedByUserId, @DefaultUserId) ELSE ApprovedByUserId END,
        ApprovedDate = CASE WHEN Status IN (N'Approved', N'Converted') THEN COALESCE(ApprovedDate, UpdatedDate, CreatedDate) ELSE ApprovedDate END,
        ConvertedByUserId = CASE WHEN Status = N'Converted' THEN COALESCE(ConvertedByUserId, @DefaultUserId) ELSE ConvertedByUserId END,
        ConvertedDate = CASE WHEN Status = N'Converted' THEN COALESCE(ConvertedDate, UpdatedDate, CreatedDate) ELSE ConvertedDate END;

    UPDATE dbo.InvoiceHeaders
    SET CreatedByUserId = COALESCE(CreatedByUserId, @DefaultUserId),
        UpdatedByUserId = CASE WHEN UpdatedDate IS NOT NULL THEN COALESCE(UpdatedByUserId, @DefaultUserId) ELSE UpdatedByUserId END,
        IssuedByUserId = CASE WHEN Status IN (N'Issued', N'PartiallyPaid', N'Paid') THEN COALESCE(IssuedByUserId, @DefaultUserId) ELSE IssuedByUserId END,
        IssuedDate = CASE WHEN Status IN (N'Issued', N'PartiallyPaid', N'Paid') THEN COALESCE(IssuedDate, CreatedDate) ELSE IssuedDate END,
        CancelledByUserId = CASE WHEN Status = N'Cancelled' THEN COALESCE(CancelledByUserId, @DefaultUserId) ELSE CancelledByUserId END,
        CancelledDate = CASE WHEN Status = N'Cancelled' THEN COALESCE(CancelledDate, UpdatedDate, CreatedDate) ELSE CancelledDate END;

    UPDATE dbo.PaymentHeaders
    SET CreatedByUserId = COALESCE(CreatedByUserId, @DefaultUserId),
        PostedByUserId = CASE WHEN Status = N'Posted' THEN COALESCE(PostedByUserId, @DefaultUserId) ELSE PostedByUserId END,
        PostedDate = CASE WHEN Status = N'Posted' THEN COALESCE(PostedDate, CreatedDate) ELSE PostedDate END,
        CancelledByUserId = CASE WHEN Status = N'Cancelled' THEN COALESCE(CancelledByUserId, @DefaultUserId) ELSE CancelledByUserId END,
        CancelledDate = CASE WHEN Status = N'Cancelled' THEN COALESCE(CancelledDate, UpdatedDate, CreatedDate) ELSE CancelledDate END;

    UPDATE dbo.ReceiptHeaders
    SET CreatedByUserId = COALESCE(CreatedByUserId, @DefaultUserId),
        IssuedByUserId = CASE WHEN Status = N'Issued' THEN COALESCE(IssuedByUserId, @DefaultUserId) ELSE IssuedByUserId END,
        IssuedDate = CASE WHEN Status = N'Issued' THEN COALESCE(IssuedDate, CreatedDate) ELSE IssuedDate END,
        CancelledByUserId = CASE WHEN Status = N'Cancelled' THEN COALESCE(CancelledByUserId, @DefaultUserId) ELSE CancelledByUserId END,
        CancelledDate = CASE WHEN Status = N'Cancelled' THEN COALESCE(CancelledDate, UpdatedDate, CreatedDate) ELSE CancelledDate END;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_QuotationHeaders_Users_CreatedByUserId')
    ALTER TABLE dbo.QuotationHeaders ADD CONSTRAINT FK_QuotationHeaders_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_QuotationHeaders_Users_UpdatedByUserId')
    ALTER TABLE dbo.QuotationHeaders ADD CONSTRAINT FK_QuotationHeaders_Users_UpdatedByUserId FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_QuotationHeaders_Users_ApprovedByUserId')
    ALTER TABLE dbo.QuotationHeaders ADD CONSTRAINT FK_QuotationHeaders_Users_ApprovedByUserId FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_QuotationHeaders_Users_ConvertedByUserId')
    ALTER TABLE dbo.QuotationHeaders ADD CONSTRAINT FK_QuotationHeaders_Users_ConvertedByUserId FOREIGN KEY (ConvertedByUserId) REFERENCES dbo.Users(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_Users_CreatedByUserId')
    ALTER TABLE dbo.InvoiceHeaders ADD CONSTRAINT FK_InvoiceHeaders_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_Users_UpdatedByUserId')
    ALTER TABLE dbo.InvoiceHeaders ADD CONSTRAINT FK_InvoiceHeaders_Users_UpdatedByUserId FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_Users_IssuedByUserId')
    ALTER TABLE dbo.InvoiceHeaders ADD CONSTRAINT FK_InvoiceHeaders_Users_IssuedByUserId FOREIGN KEY (IssuedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InvoiceHeaders_Users_CancelledByUserId')
    ALTER TABLE dbo.InvoiceHeaders ADD CONSTRAINT FK_InvoiceHeaders_Users_CancelledByUserId FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentHeaders_Users_CreatedByUserId')
    ALTER TABLE dbo.PaymentHeaders ADD CONSTRAINT FK_PaymentHeaders_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentHeaders_Users_PostedByUserId')
    ALTER TABLE dbo.PaymentHeaders ADD CONSTRAINT FK_PaymentHeaders_Users_PostedByUserId FOREIGN KEY (PostedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentHeaders_Users_CancelledByUserId')
    ALTER TABLE dbo.PaymentHeaders ADD CONSTRAINT FK_PaymentHeaders_Users_CancelledByUserId FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceiptHeaders_Users_CreatedByUserId')
    ALTER TABLE dbo.ReceiptHeaders ADD CONSTRAINT FK_ReceiptHeaders_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceiptHeaders_Users_IssuedByUserId')
    ALTER TABLE dbo.ReceiptHeaders ADD CONSTRAINT FK_ReceiptHeaders_Users_IssuedByUserId FOREIGN KEY (IssuedByUserId) REFERENCES dbo.Users(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceiptHeaders_Users_CancelledByUserId')
    ALTER TABLE dbo.ReceiptHeaders ADD CONSTRAINT FK_ReceiptHeaders_Users_CancelledByUserId FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId);
GO
