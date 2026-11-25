/*
  Gestión Empresarial RD - esquema de negocio para SQL Server.
  Nota: ASP.NET Core Identity administra sus propias tablas AspNet* mediante EF Core.
  Ejecutar sobre una instancia de SQL Server con permisos para crear bases de datos.
  Para el esquema de ejecución del proyecto, preferir migraciones EF Core como fuente de verdad.
*/
IF DB_ID(N'GestionEmpresarialRD') IS NULL
BEGIN
    CREATE DATABASE GestionEmpresarialRD;
END;
GO
USE GestionEmpresarialRD;
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        CategoryId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        Name             NVARCHAR(80) NOT NULL,
        Description      NVARCHAR(250) NULL,
        IsActive         BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
        CreatedAtUtc     DATETIME2(0) NOT NULL CONSTRAINT DF_Categories_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Categories_Name UNIQUE (Name)
    );
END;
GO

IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suppliers
    (
        SupplierId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
        Name             NVARCHAR(120) NOT NULL,
        TaxId            NVARCHAR(20) NULL,
        Phone            NVARCHAR(30) NULL,
        Email            NVARCHAR(254) NULL,
        Address          NVARCHAR(250) NULL,
        IsActive         BIT NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),
        CreatedAtUtc     DATETIME2(0) NOT NULL CONSTRAINT DF_Suppliers_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_Suppliers_Name ON dbo.Suppliers(Name);
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        SKU              NVARCHAR(40) NOT NULL,
        Name             NVARCHAR(140) NOT NULL,
        Description      NVARCHAR(500) NULL,
        CategoryId       INT NOT NULL,
        CostPrice        DECIMAL(18,2) NOT NULL,
        SalePrice        DECIMAL(18,2) NOT NULL,
        CurrentStock     INT NOT NULL CONSTRAINT DF_Products_CurrentStock DEFAULT (0),
        MinimumStock     INT NOT NULL CONSTRAINT DF_Products_MinimumStock DEFAULT (0),
        IsActive         BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedAtUtc     DATETIME2(0) NOT NULL CONSTRAINT DF_Products_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc     DATETIME2(0) NOT NULL CONSTRAINT DF_Products_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        RowVersion       ROWVERSION NOT NULL,
        CONSTRAINT UQ_Products_SKU UNIQUE (SKU),
        CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
        CONSTRAINT CK_Products_CostPrice CHECK (CostPrice >= 0),
        CONSTRAINT CK_Products_SalePrice CHECK (SalePrice >= 0),
        CONSTRAINT CK_Products_CurrentStock CHECK (CurrentStock >= 0),
        CONSTRAINT CK_Products_MinimumStock CHECK (MinimumStock >= 0)
    );
    CREATE INDEX IX_Products_Name ON dbo.Products(Name);
    CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
    CREATE INDEX IX_Products_LowStock ON dbo.Products(CurrentStock, MinimumStock) WHERE IsActive = 1;
END;
GO

IF OBJECT_ID(N'dbo.Purchases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Purchases
    (
        PurchaseId       BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Purchases PRIMARY KEY,
        PurchaseNumber   NVARCHAR(30) NOT NULL,
        SupplierId       INT NOT NULL,
        Status           VARCHAR(12) NOT NULL CONSTRAINT DF_Purchases_Status DEFAULT ('Draft'),
        Subtotal         DECIMAL(18,2) NOT NULL,
        Total             DECIMAL(18,2) NOT NULL,
        CreatedByUserId  NVARCHAR(450) NOT NULL,
        CreatedAtUtc     DATETIME2(0) NOT NULL CONSTRAINT DF_Purchases_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        ReceivedAtUtc    DATETIME2(0) NULL,
        CONSTRAINT UQ_Purchases_PurchaseNumber UNIQUE (PurchaseNumber),
        CONSTRAINT FK_Purchases_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(SupplierId),
        CONSTRAINT CK_Purchases_Status CHECK (Status IN ('Draft','Received','Cancelled')),
        CONSTRAINT CK_Purchases_Totals CHECK (Subtotal >= 0 AND Total >= 0)
    );
    CREATE INDEX IX_Purchases_SupplierId_CreatedAtUtc ON dbo.Purchases(SupplierId, CreatedAtUtc DESC);
END;
GO

IF OBJECT_ID(N'dbo.PurchaseItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseItems
    (
        PurchaseItemId   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseItems PRIMARY KEY,
        PurchaseId       BIGINT NOT NULL,
        ProductId        INT NOT NULL,
        Quantity         INT NOT NULL,
        UnitCost         DECIMAL(18,2) NOT NULL,
        LineTotal        DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_PurchaseItems_Purchases FOREIGN KEY (PurchaseId) REFERENCES dbo.Purchases(PurchaseId),
        CONSTRAINT FK_PurchaseItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        CONSTRAINT CK_PurchaseItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_PurchaseItems_UnitCost CHECK (UnitCost >= 0),
        CONSTRAINT CK_PurchaseItems_LineTotal CHECK (LineTotal >= 0)
    );
    CREATE INDEX IX_PurchaseItems_PurchaseId ON dbo.PurchaseItems(PurchaseId);
    CREATE INDEX IX_PurchaseItems_ProductId ON dbo.PurchaseItems(ProductId);
END;
GO

IF OBJECT_ID(N'dbo.Sales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sales
    (
        SaleId            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sales PRIMARY KEY,
        SaleNumber        NVARCHAR(30) NOT NULL,
        Status            VARCHAR(12) NOT NULL CONSTRAINT DF_Sales_Status DEFAULT ('Completed'),
        PaymentMethod     VARCHAR(12) NOT NULL,
        Subtotal          DECIMAL(18,2) NOT NULL,
        Discount          DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sales_Discount DEFAULT (0),
        Total             DECIMAL(18,2) NOT NULL,
        CreatedByUserId   NVARCHAR(450) NOT NULL,
        CreatedAtUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_Sales_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Sales_SaleNumber UNIQUE (SaleNumber),
        CONSTRAINT CK_Sales_Status CHECK (Status IN ('Completed','Cancelled')),
        CONSTRAINT CK_Sales_PaymentMethod CHECK (PaymentMethod IN ('Cash','Card','Transfer')),
        CONSTRAINT CK_Sales_Totals CHECK (Subtotal >= 0 AND Discount >= 0 AND Total >= 0 AND Total = Subtotal - Discount)
    );
    CREATE INDEX IX_Sales_CreatedAtUtc ON dbo.Sales(CreatedAtUtc DESC);
    CREATE INDEX IX_Sales_CreatedByUserId ON dbo.Sales(CreatedByUserId);
END;
GO

IF OBJECT_ID(N'dbo.SaleItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleItems
    (
        SaleItemId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SaleItems PRIMARY KEY,
        SaleId            BIGINT NOT NULL,
        ProductId         INT NOT NULL,
        ProductName       NVARCHAR(140) NOT NULL,
        SKU               NVARCHAR(40) NOT NULL,
        Quantity          INT NOT NULL,
        UnitPrice         DECIMAL(18,2) NOT NULL,
        LineTotal         DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(SaleId),
        CONSTRAINT FK_SaleItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        CONSTRAINT CK_SaleItems_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_SaleItems_UnitPrice CHECK (UnitPrice >= 0),
        CONSTRAINT CK_SaleItems_LineTotal CHECK (LineTotal >= 0)
    );
    CREATE INDEX IX_SaleItems_SaleId ON dbo.SaleItems(SaleId);
    CREATE INDEX IX_SaleItems_ProductId ON dbo.SaleItems(ProductId);
END;
GO

IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovements
    (
        StockMovementId  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovements PRIMARY KEY,
        ProductId        INT NOT NULL,
        MovementType     VARCHAR(12) NOT NULL,
        QuantityDelta    INT NOT NULL,
        StockBefore      INT NOT NULL,
        StockAfter       INT NOT NULL,
        ReferenceType    VARCHAR(12) NOT NULL,
        ReferenceId      BIGINT NULL,
        Notes            NVARCHAR(250) NULL,
        CreatedByUserId  NVARCHAR(450) NOT NULL,
        CreatedAtUtc     DATETIME2(0) NOT NULL CONSTRAINT DF_StockMovements_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StockMovements_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        CONSTRAINT CK_StockMovements_Type CHECK (MovementType IN ('Purchase','Sale','Adjustment')),
        CONSTRAINT CK_StockMovements_ReferenceType CHECK (ReferenceType IN ('Purchase','Sale','Manual')),
        CONSTRAINT CK_StockMovements_Stock CHECK (StockBefore >= 0 AND StockAfter >= 0)
    );
    CREATE INDEX IX_StockMovements_ProductId_CreatedAtUtc ON dbo.StockMovements(ProductId, CreatedAtUtc DESC);
    CREATE INDEX IX_StockMovements_Reference ON dbo.StockMovements(ReferenceType, ReferenceId);
END;
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditLogId        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        UserId            NVARCHAR(450) NOT NULL,
        Action            NVARCHAR(80) NOT NULL,
        EntityName        NVARCHAR(80) NOT NULL,
        EntityId          NVARCHAR(80) NULL,
        DetailsJson       NVARCHAR(MAX) NULL,
        IpAddress         NVARCHAR(45) NULL,
        CreatedAtUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_AuditLogs_DetailsJson CHECK (DetailsJson IS NULL OR ISJSON(DetailsJson) = 1)
    );
    CREATE INDEX IX_AuditLogs_CreatedAtUtc ON dbo.AuditLogs(CreatedAtUtc DESC);
    CREATE INDEX IX_AuditLogs_UserId_CreatedAtUtc ON dbo.AuditLogs(UserId, CreatedAtUtc DESC);
END;
GO

PRINT N'Esquema de negocio creado o ya existente. Configura Identity y las migraciones EF Core antes de conectar la API.';
GO
