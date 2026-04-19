CREATE TABLE dbo.QuotationHeaders
(
    QuotationHeaderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_QuotationHeaders PRIMARY KEY,
    QuotationNumber NVARCHAR(30) NOT NULL,
    QuotationDate DATE NOT NULL,
    ExpiryDate DATE NULL,
    CustomerId INT NOT NULL,
    SalespersonId INT NULL,
    ReferenceNo NVARCHAR(50) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_QuotationHeaders_Status DEFAULT (N'Draft'),
    Remarks NVARCHAR(500) NULL,
    Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationHeaders_Subtotal DEFAULT (0),
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationHeaders_DiscountAmount DEFAULT (0),
    DiscountMode NVARCHAR(10) NOT NULL CONSTRAINT DF_QuotationHeaders_DiscountMode DEFAULT (N'Line'),
    HeaderDiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationHeaders_HeaderDiscountAmount DEFAULT (0),
    VatType NVARCHAR(10) NOT NULL CONSTRAINT DF_QuotationHeaders_VatType DEFAULT (N'NoVAT'),
    VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationHeaders_VatAmount DEFAULT (0),
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationHeaders_TotalAmount DEFAULT (0),
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_QuotationHeaders_CreatedDate DEFAULT (SYSUTCDATETIME()),
    UpdatedDate DATETIME2 NULL,
    CONSTRAINT FK_QuotationHeaders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_QuotationHeaders_Salespersons FOREIGN KEY (SalespersonId) REFERENCES dbo.Salespersons (SalespersonId)
);
GO

CREATE UNIQUE INDEX UX_QuotationHeaders_Number ON dbo.QuotationHeaders (QuotationNumber);
GO

CREATE TABLE dbo.QuotationDetails
(
    QuotationDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_QuotationDetails PRIMARY KEY,
    QuotationHeaderId INT NOT NULL,
    LineNumber INT NOT NULL,
    ItemId INT NOT NULL,
    Description NVARCHAR(1000) NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuotationDetails_DiscountAmount DEFAULT (0),
    LineTotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_QuotationDetails_QuotationHeaders FOREIGN KEY (QuotationHeaderId) REFERENCES dbo.QuotationHeaders (QuotationHeaderId) ON DELETE CASCADE,
    CONSTRAINT FK_QuotationDetails_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items (ItemId),
    CONSTRAINT CK_QuotationDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_QuotationDetails_Discount CHECK (DiscountAmount >= 0 AND DiscountAmount <= (Quantity * UnitPrice))
);
GO

CREATE INDEX IX_QuotationDetails_Header ON dbo.QuotationDetails (QuotationHeaderId, LineNumber);
GO
