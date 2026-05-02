IF OBJECT_ID(N'dbo.PriceLevels', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriceLevels
    (
        PriceLevelId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PriceLevels PRIMARY KEY,
        PriceLevelCode NVARCHAR(30) NOT NULL,
        PriceLevelName NVARCHAR(80) NOT NULL,
        Description NVARCHAR(250) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_PriceLevels_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_PriceLevels_IsActive DEFAULT (1),
        CONSTRAINT UX_PriceLevels_Code UNIQUE (PriceLevelCode)
    );
END;
GO

IF OBJECT_ID(N'dbo.ItemPrices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemPrices
    (
        ItemPriceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ItemPrices PRIMARY KEY,
        ItemId INT NOT NULL,
        PriceLevelId INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_ItemPrices_UnitPrice DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_ItemPrices_IsActive DEFAULT (1),
        CONSTRAINT FK_ItemPrices_Items FOREIGN KEY (ItemId) REFERENCES dbo.Items (ItemId),
        CONSTRAINT FK_ItemPrices_PriceLevels FOREIGN KEY (PriceLevelId) REFERENCES dbo.PriceLevels (PriceLevelId),
        CONSTRAINT UX_ItemPrices_ItemId_PriceLevelId UNIQUE (ItemId, PriceLevelId)
    );
END;
GO
