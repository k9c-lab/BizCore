IF OBJECT_ID(N'dbo.ReceiptPrintLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceiptPrintLines
    (
        ReceiptPrintLineId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceiptPrintLines PRIMARY KEY,
        ReceiptId INT NOT NULL,
        LineNumber INT NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_ReceiptPrintLines_ReceiptHeaders_ReceiptId
            FOREIGN KEY (ReceiptId) REFERENCES dbo.ReceiptHeaders (ReceiptId) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_ReceiptPrintLines_ReceiptId_LineNumber
        ON dbo.ReceiptPrintLines (ReceiptId, LineNumber);
END;
