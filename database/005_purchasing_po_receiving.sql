CREATE TABLE dbo.PurchaseOrderHeaders
(
    PurchaseOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrderHeaders PRIMARY KEY,
    PONo NVARCHAR(30) NOT NULL,
    PODate DATETIME2 NOT NULL,
    SupplierId INT NOT NULL,
    ReferenceNo NVARCHAR(50) NULL,
    ExpectedReceiveDate DATETIME2 NULL,
    Remark NVARCHAR(500) NULL,
    Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseOrderHeaders_Subtotal DEFAULT (0),
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseOrderHeaders_Discount DEFAULT (0),
    VatType NVARCHAR(10) NOT NULL CONSTRAINT DF_PurchaseOrderHeaders_VatType DEFAULT (N'VAT'),
    VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseOrderHeaders_Vat DEFAULT (0),
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseOrderHeaders_Total DEFAULT (0),
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PurchaseOrderHeaders_Status DEFAULT (N'Draft'),
    CreatedByUserId INT NULL,
    UpdatedByUserId INT NULL,
    ApprovedByUserId INT NULL,
    ApprovedDate DATETIME2 NULL,
    CancelledByUserId INT NULL,
    CancelledDate DATETIME2 NULL,
    CancelReason NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_PurchaseOrderHeaders_Created DEFAULT (SYSUTCDATETIME()),
    UpdatedDate DATETIME2 NULL,
    CONSTRAINT FK_PurchaseOrderHeaders_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (SupplierId),
    CONSTRAINT FK_PurchaseOrderHeaders_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_PurchaseOrderHeaders_UpdatedByUsers FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_PurchaseOrderHeaders_ApprovedByUsers FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_PurchaseOrderHeaders_CancelledByUsers FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users (UserId)
);
GO

CREATE UNIQUE INDEX UX_PurchaseOrderHeaders_PONo ON dbo.PurchaseOrderHeaders (PONo);
GO

CREATE TABLE dbo.PurchaseOrderDetails
(
    PurchaseOrderDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrderDetails PRIMARY KEY,
    PurchaseOrderId INT NOT NULL,
    LineNumber INT NOT NULL,
    ItemId INT NOT NULL,
    Qty DECIMAL(18,2) NOT NULL,
    ReceivedQty DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseOrderDetails_ReceivedQty DEFAULT (0),
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseOrderDetails_Discount DEFAULT (0),
    LineTotal DECIMAL(18,2) NOT NULL,
    Remark NVARCHAR(300) NULL,
    CONSTRAINT FK_PurchaseOrderDetails_Headers FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrderHeaders (PurchaseOrderId) ON DELETE CASCADE,
    CONSTRAINT FK_PurchaseOrderDetails_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items (ItemId),
    CONSTRAINT CK_PurchaseOrderDetails_Qty CHECK (Qty > 0),
    CONSTRAINT CK_PurchaseOrderDetails_ReceivedQty CHECK (ReceivedQty >= 0 AND ReceivedQty <= Qty)
);
GO

CREATE INDEX IX_PurchaseOrderDetails_Header ON dbo.PurchaseOrderDetails (PurchaseOrderId, LineNumber);
GO

CREATE TABLE dbo.ReceivingHeaders
(
    ReceivingId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceivingHeaders PRIMARY KEY,
    ReceivingNo NVARCHAR(30) NOT NULL,
    ReceiveDate DATETIME2 NOT NULL,
    SupplierId INT NOT NULL,
    PurchaseOrderId INT NOT NULL,
    DeliveryNoteNo NVARCHAR(50) NULL,
    Remark NVARCHAR(500) NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ReceivingHeaders_Status DEFAULT (N'Posted'),
    CreatedByUserId INT NULL,
    PostedByUserId INT NULL,
    PostedDate DATETIME2 NULL,
    CancelledByUserId INT NULL,
    CancelledDate DATETIME2 NULL,
    CancelReason NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_ReceivingHeaders_Created DEFAULT (SYSUTCDATETIME()),
    UpdatedDate DATETIME2 NULL,
    CONSTRAINT FK_ReceivingHeaders_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (SupplierId),
    CONSTRAINT FK_ReceivingHeaders_PurchaseOrders FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrderHeaders (PurchaseOrderId),
    CONSTRAINT FK_ReceivingHeaders_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_ReceivingHeaders_PostedByUsers FOREIGN KEY (PostedByUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_ReceivingHeaders_CancelledByUsers FOREIGN KEY (CancelledByUserId) REFERENCES dbo.Users (UserId)
);
GO

CREATE UNIQUE INDEX UX_ReceivingHeaders_ReceivingNo ON dbo.ReceivingHeaders (ReceivingNo);
GO

CREATE TABLE dbo.ReceivingDetails
(
    ReceivingDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceivingDetails PRIMARY KEY,
    ReceivingId INT NOT NULL,
    PurchaseOrderDetailId INT NOT NULL,
    ItemId INT NOT NULL,
    LineNumber INT NOT NULL,
    QtyReceived DECIMAL(18,2) NOT NULL,
    Remark NVARCHAR(300) NULL,
    CONSTRAINT FK_ReceivingDetails_Headers FOREIGN KEY (ReceivingId) REFERENCES dbo.ReceivingHeaders (ReceivingId) ON DELETE CASCADE,
    CONSTRAINT FK_ReceivingDetails_PODetails FOREIGN KEY (PurchaseOrderDetailId) REFERENCES dbo.PurchaseOrderDetails (PurchaseOrderDetailId),
    CONSTRAINT FK_ReceivingDetails_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items (ItemId),
    CONSTRAINT CK_ReceivingDetails_QtyReceived CHECK (QtyReceived > 0)
);
GO

CREATE INDEX IX_ReceivingDetails_Header ON dbo.ReceivingDetails (ReceivingId, LineNumber);
GO

CREATE TABLE dbo.ReceivingSerials
(
    ReceivingSerialId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceivingSerials PRIMARY KEY,
    ReceivingDetailId INT NOT NULL,
    ItemId INT NOT NULL,
    SerialNo NVARCHAR(120) NOT NULL,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_ReceivingSerials_Created DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_ReceivingSerials_Details FOREIGN KEY (ReceivingDetailId) REFERENCES dbo.ReceivingDetails (ReceivingDetailId) ON DELETE CASCADE,
    CONSTRAINT FK_ReceivingSerials_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items (ItemId)
);
GO

CREATE INDEX IX_ReceivingSerials_Detail ON dbo.ReceivingSerials (ReceivingDetailId);
GO
