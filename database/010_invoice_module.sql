CREATE TABLE dbo.InvoiceHeaders
(
    InvoiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceHeaders PRIMARY KEY,
    InvoiceNo NVARCHAR(30) NOT NULL,
    InvoiceDate DATETIME2 NOT NULL,
    CustomerId INT NOT NULL,
    SalespersonId INT NULL,
    QuotationId INT NULL,
    ReferenceNo NVARCHAR(50) NULL,
    Remark NVARCHAR(500) NULL,
    Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceHeaders_Subtotal DEFAULT ((0)),
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceHeaders_DiscountAmount DEFAULT ((0)),
    VatType NVARCHAR(10) NOT NULL CONSTRAINT DF_InvoiceHeaders_VatType DEFAULT (N'NoVAT'),
    VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceHeaders_VatAmount DEFAULT ((0)),
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceHeaders_TotalAmount DEFAULT ((0)),
    PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceHeaders_PaidAmount DEFAULT ((0)),
    BalanceAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceHeaders_BalanceAmount DEFAULT ((0)),
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_InvoiceHeaders_Status DEFAULT (N'Issued'),
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_InvoiceHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
    UpdatedDate DATETIME2 NULL,
    CONSTRAINT FK_InvoiceHeaders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_InvoiceHeaders_Salespersons FOREIGN KEY (SalespersonId) REFERENCES dbo.Salespersons (SalespersonId),
    CONSTRAINT FK_InvoiceHeaders_QuotationHeaders FOREIGN KEY (QuotationId) REFERENCES dbo.QuotationHeaders (QuotationHeaderId)
);
GO

CREATE UNIQUE INDEX UX_InvoiceHeaders_InvoiceNo ON dbo.InvoiceHeaders (InvoiceNo);
GO

CREATE TABLE dbo.InvoiceDetails
(
    InvoiceDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceDetails PRIMARY KEY,
    InvoiceId INT NOT NULL,
    LineNumber INT NOT NULL,
    ItemId INT NOT NULL,
    Qty DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceDetails_DiscountAmount DEFAULT ((0)),
    LineTotal DECIMAL(18,2) NOT NULL,
    Remark NVARCHAR(500) NULL,
    CustomerWarrantyStartDate DATETIME2 NULL,
    CustomerWarrantyEndDate DATETIME2 NULL,
    CONSTRAINT FK_InvoiceDetails_Headers FOREIGN KEY (InvoiceId) REFERENCES dbo.InvoiceHeaders (InvoiceId) ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceDetails_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items (ItemId)
);
GO

CREATE TABLE dbo.InvoiceSerials
(
    InvoiceSerialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceSerials PRIMARY KEY,
    InvoiceDetailId INT NOT NULL,
    SerialId INT NOT NULL,
    CONSTRAINT FK_InvoiceSerials_Details FOREIGN KEY (InvoiceDetailId) REFERENCES dbo.InvoiceDetails (InvoiceDetailId) ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceSerials_Serials FOREIGN KEY (SerialId) REFERENCES dbo.SerialNumbers (SerialId)
);
GO

CREATE UNIQUE INDEX UX_InvoiceSerials_DetailSerial ON dbo.InvoiceSerials (InvoiceDetailId, SerialId);
GO

CREATE UNIQUE INDEX UX_InvoiceSerials_SerialId ON dbo.InvoiceSerials (SerialId);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_SerialNumbers_InvoiceHeaders'
)
BEGIN
    ALTER TABLE dbo.SerialNumbers
    ADD CONSTRAINT FK_SerialNumbers_InvoiceHeaders
        FOREIGN KEY (InvoiceId) REFERENCES dbo.InvoiceHeaders (InvoiceId);
END;
GO
