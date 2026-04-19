CREATE TABLE dbo.PaymentHeaders
(
    PaymentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentHeaders PRIMARY KEY,
    PaymentNo NVARCHAR(30) NOT NULL,
    PaymentDate DATETIME2 NOT NULL,
    CustomerId INT NOT NULL,
    PaymentMethod NVARCHAR(20) NOT NULL,
    ReferenceNo NVARCHAR(100) NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Remark NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PaymentHeaders_Status DEFAULT (N'Posted'),
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_PaymentHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
    UpdatedDate DATETIME2 NULL,
    CONSTRAINT FK_PaymentHeaders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId)
);
GO

CREATE UNIQUE INDEX UX_PaymentHeaders_PaymentNo ON dbo.PaymentHeaders (PaymentNo);
GO

CREATE TABLE dbo.PaymentAllocations
(
    PaymentAllocationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentAllocations PRIMARY KEY,
    PaymentId INT NOT NULL,
    InvoiceId INT NOT NULL,
    AppliedAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_PaymentAllocations_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.PaymentHeaders (PaymentId) ON DELETE CASCADE,
    CONSTRAINT FK_PaymentAllocations_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.InvoiceHeaders (InvoiceId)
);
GO

CREATE UNIQUE INDEX UX_PaymentAllocations_PaymentInvoice ON dbo.PaymentAllocations (PaymentId, InvoiceId);
GO

CREATE TABLE dbo.ReceiptHeaders
(
    ReceiptId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceiptHeaders PRIMARY KEY,
    ReceiptNo NVARCHAR(30) NOT NULL,
    ReceiptDate DATETIME2 NOT NULL,
    CustomerId INT NOT NULL,
    PaymentId INT NOT NULL,
    TotalReceivedAmount DECIMAL(18,2) NOT NULL,
    Remark NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ReceiptHeaders_Status DEFAULT (N'Issued'),
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_ReceiptHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
    UpdatedDate DATETIME2 NULL,
    CONSTRAINT FK_ReceiptHeaders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_ReceiptHeaders_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.PaymentHeaders (PaymentId)
);
GO

CREATE UNIQUE INDEX UX_ReceiptHeaders_ReceiptNo ON dbo.ReceiptHeaders (ReceiptNo);
GO

CREATE UNIQUE INDEX UX_ReceiptHeaders_PaymentId ON dbo.ReceiptHeaders (PaymentId);
GO
