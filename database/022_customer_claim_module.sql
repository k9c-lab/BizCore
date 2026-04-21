-- Customer Claim / RMA Phase 1.
-- Safe to run multiple times.

IF OBJECT_ID('dbo.CustomerClaimHeaders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerClaimHeaders
    (
        CustomerClaimId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerClaimHeaders PRIMARY KEY,
        CustomerClaimNo NVARCHAR(30) NOT NULL,
        CustomerClaimDate DATETIME2 NOT NULL,
        CustomerId INT NOT NULL,
        InvoiceId INT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_CustomerClaimHeaders_Status DEFAULT (N'Open'),
        ProblemDescription NVARCHAR(1000) NULL,
        ResolutionRemark NVARCHAR(1000) NULL,
        CancelReason NVARCHAR(500) NULL,
        CreatedByUserId INT NULL,
        UpdatedByUserId INT NULL,
        CancelledByUserId INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_CustomerClaimHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CancelledDate DATETIME2 NULL,
        CONSTRAINT FK_CustomerClaimHeaders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_CustomerClaimHeaders_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.InvoiceHeaders(InvoiceId),
        CONSTRAINT FK_CustomerClaimHeaders_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_CustomerClaimHeaders_Users_UpdatedByUserId FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_CustomerClaimHeaders_Users_CancelledByUserId FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users(UserId)
    );

    CREATE UNIQUE INDEX UX_CustomerClaimHeaders_CustomerClaimNo ON dbo.CustomerClaimHeaders(CustomerClaimNo);
END;
GO

IF OBJECT_ID('dbo.CustomerClaimDetails', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerClaimDetails
    (
        CustomerClaimDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerClaimDetails PRIMARY KEY,
        CustomerClaimId INT NOT NULL,
        SerialId INT NOT NULL,
        ItemId INT NOT NULL,
        OriginalInvoiceId INT NULL,
        ReplacementSerialId INT NULL,
        LineRemark NVARCHAR(500) NULL,
        CONSTRAINT FK_CustomerClaimDetails_Headers FOREIGN KEY (CustomerClaimId) REFERENCES dbo.CustomerClaimHeaders(CustomerClaimId) ON DELETE CASCADE,
        CONSTRAINT FK_CustomerClaimDetails_Serials FOREIGN KEY (SerialId) REFERENCES dbo.SerialNumbers(SerialId),
        CONSTRAINT FK_CustomerClaimDetails_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items(ItemId),
        CONSTRAINT FK_CustomerClaimDetails_OriginalInvoices FOREIGN KEY (OriginalInvoiceId) REFERENCES dbo.InvoiceHeaders(InvoiceId),
        CONSTRAINT FK_CustomerClaimDetails_ReplacementSerials FOREIGN KEY (ReplacementSerialId) REFERENCES dbo.SerialNumbers(SerialId)
    );

    CREATE INDEX IX_CustomerClaimDetails_CustomerClaimId ON dbo.CustomerClaimDetails(CustomerClaimId);
    CREATE INDEX IX_CustomerClaimDetails_SerialId ON dbo.CustomerClaimDetails(SerialId);
END;
GO
