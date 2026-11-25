IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [dbo].[Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [Description] nvarchar(250) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [CK_Categories_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
);

CREATE TABLE [dbo].[Suppliers] (
    [SupplierId] int NOT NULL IDENTITY,
    [Name] nvarchar(120) NOT NULL,
    [TaxId] nvarchar(20) NULL,
    [Phone] nvarchar(30) NULL,
    [Email] nvarchar(254) NULL,
    [Address] nvarchar(250) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([SupplierId]),
    CONSTRAINT [CK_Suppliers_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[AuditLogs] (
    [AuditLogId] bigint NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [Action] nvarchar(80) NOT NULL,
    [EntityName] nvarchar(80) NOT NULL,
    [EntityId] nvarchar(80) NULL,
    [DetailsJson] nvarchar(max) NULL,
    [IpAddress] nvarchar(45) NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId]),
    CONSTRAINT [CK_AuditLogs_Action_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Action]))) > 0),
    CONSTRAINT [CK_AuditLogs_DetailsJson_Valid] CHECK ([DetailsJson] IS NULL OR ISJSON([DetailsJson]) = 1),
    CONSTRAINT [CK_AuditLogs_EntityName_NotEmpty] CHECK (LEN(LTRIM(RTRIM([EntityName]))) > 0),
    CONSTRAINT [FK_AuditLogs_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[Sales] (
    [SaleId] bigint NOT NULL IDENTITY,
    [SaleNumber] nvarchar(30) NOT NULL,
    [Status] varchar(12) NOT NULL DEFAULT 'Completed',
    [Subtotal] decimal(18,2) NOT NULL,
    [Discount] decimal(18,2) NOT NULL DEFAULT 0.0,
    [Total] decimal(18,2) NOT NULL,
    [PaymentMethod] varchar(24) NOT NULL,
    [CreatedByUserId] nvarchar(450) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Sales] PRIMARY KEY ([SaleId]),
    CONSTRAINT [CK_Sales_Discount_NonNegative] CHECK ([Discount] >= 0),
    CONSTRAINT [CK_Sales_Discount_NotGreaterThanSubtotal] CHECK ([Discount] <= [Subtotal]),
    CONSTRAINT [CK_Sales_PaymentMethod_Valid] CHECK ([PaymentMethod] IN ('Cash', 'Card', 'Transfer')),
    CONSTRAINT [CK_Sales_Status_Valid] CHECK ([Status] IN ('Completed', 'Cancelled')),
    CONSTRAINT [CK_Sales_Subtotal_NonNegative] CHECK ([Subtotal] >= 0),
    CONSTRAINT [CK_Sales_Total_MatchesSubtotalAndDiscount] CHECK ([Total] = [Subtotal] - [Discount]),
    CONSTRAINT [CK_Sales_Total_NonNegative] CHECK ([Total] >= 0),
    CONSTRAINT [FK_Sales_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[Products] (
    [ProductId] int NOT NULL IDENTITY,
    [SKU] nvarchar(40) NOT NULL,
    [Name] nvarchar(140) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CategoryId] int NOT NULL,
    [CostPrice] decimal(18,2) NOT NULL,
    [SalePrice] decimal(18,2) NOT NULL,
    [CurrentStock] int NOT NULL DEFAULT 0,
    [MinimumStock] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([ProductId]),
    CONSTRAINT [CK_Products_CostPrice_NonNegative] CHECK ([CostPrice] >= 0),
    CONSTRAINT [CK_Products_CurrentStock_NonNegative] CHECK ([CurrentStock] >= 0),
    CONSTRAINT [CK_Products_MinimumStock_NonNegative] CHECK ([MinimumStock] >= 0),
    CONSTRAINT [CK_Products_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Products_SalePrice_NonNegative] CHECK ([SalePrice] >= 0),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([CategoryId]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[Purchases] (
    [PurchaseId] bigint NOT NULL IDENTITY,
    [PurchaseNumber] nvarchar(30) NOT NULL,
    [SupplierId] int NOT NULL,
    [Status] varchar(12) NOT NULL DEFAULT 'Draft',
    [Subtotal] decimal(18,2) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [CreatedByUserId] nvarchar(450) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ReceivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Purchases] PRIMARY KEY ([PurchaseId]),
    CONSTRAINT [CK_Purchases_Status_Valid] CHECK ([Status] IN ('Draft', 'Received', 'Cancelled')),
    CONSTRAINT [CK_Purchases_Totals_NonNegative] CHECK ([Subtotal] >= 0 AND [Total] >= 0),
    CONSTRAINT [FK_Purchases_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Purchases_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([SupplierId]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[SaleItems] (
    [SaleItemId] bigint NOT NULL IDENTITY,
    [SaleId] bigint NOT NULL,
    [ProductId] int NOT NULL,
    [ProductName] nvarchar(140) NOT NULL,
    [SKU] nvarchar(40) NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_SaleItems] PRIMARY KEY ([SaleItemId]),
    CONSTRAINT [CK_SaleItems_LineTotal_NonNegative] CHECK ([LineTotal] >= 0),
    CONSTRAINT [CK_SaleItems_ProductName_NotEmpty] CHECK (LEN(LTRIM(RTRIM([ProductName]))) > 0),
    CONSTRAINT [CK_SaleItems_Quantity_Positive] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_SaleItems_SKU_NotEmpty] CHECK (LEN(LTRIM(RTRIM([SKU]))) > 0),
    CONSTRAINT [CK_SaleItems_UnitPrice_NonNegative] CHECK ([UnitPrice] >= 0),
    CONSTRAINT [FK_SaleItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([ProductId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SaleItems_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [dbo].[Sales] ([SaleId]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[StockMovements] (
    [StockMovementId] bigint NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [MovementType] varchar(12) NOT NULL,
    [QuantityDelta] int NOT NULL,
    [StockBefore] int NOT NULL,
    [StockAfter] int NOT NULL,
    [ReferenceType] varchar(12) NOT NULL,
    [ReferenceId] bigint NULL,
    [Notes] nvarchar(250) NULL,
    [CreatedByUserId] nvarchar(450) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_StockMovements] PRIMARY KEY ([StockMovementId]),
    CONSTRAINT [CK_StockMovements_Adjustment_RequiresNotes] CHECK ([MovementType] <> 'Adjustment' OR LEN(LTRIM(RTRIM(COALESCE([Notes], N'')))) > 0),
    CONSTRAINT [CK_StockMovements_Balance_MatchesDelta] CHECK ([StockAfter] = [StockBefore] + [QuantityDelta]),
    CONSTRAINT [CK_StockMovements_QuantityDelta_NonZero] CHECK ([QuantityDelta] <> 0),
    CONSTRAINT [CK_StockMovements_ReferenceType_Valid] CHECK ([ReferenceType] IN ('Purchase', 'Sale', 'Manual')),
    CONSTRAINT [CK_StockMovements_StockAfter_NonNegative] CHECK ([StockAfter] >= 0),
    CONSTRAINT [CK_StockMovements_StockBefore_NonNegative] CHECK ([StockBefore] >= 0),
    CONSTRAINT [CK_StockMovements_Type_Valid] CHECK ([MovementType] IN ('Purchase', 'Sale', 'Adjustment')),
    CONSTRAINT [FK_StockMovements_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StockMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([ProductId]) ON DELETE NO ACTION
);

CREATE TABLE [dbo].[PurchaseItems] (
    [PurchaseItemId] bigint NOT NULL IDENTITY,
    [PurchaseId] bigint NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [LineTotal] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_PurchaseItems] PRIMARY KEY ([PurchaseItemId]),
    CONSTRAINT [CK_PurchaseItems_LineTotal_NonNegative] CHECK ([LineTotal] >= 0),
    CONSTRAINT [CK_PurchaseItems_Quantity_Positive] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_PurchaseItems_UnitCost_NonNegative] CHECK ([UnitCost] >= 0),
    CONSTRAINT [FK_PurchaseItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([ProductId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PurchaseItems_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [dbo].[Purchases] ([PurchaseId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_AuditLogs_CreatedAtUtc] ON [dbo].[AuditLogs] ([CreatedAtUtc]);

CREATE INDEX [IX_AuditLogs_UserId_CreatedAtUtc] ON [dbo].[AuditLogs] ([UserId], [CreatedAtUtc]);

CREATE UNIQUE INDEX [UX_Categories_Name] ON [dbo].[Categories] ([Name]);

CREATE INDEX [IX_Products_CategoryId] ON [dbo].[Products] ([CategoryId]);

CREATE INDEX [IX_Products_Name] ON [dbo].[Products] ([Name]);

CREATE INDEX [IX_Products_Stock_MinimumStock_Active] ON [dbo].[Products] ([CurrentStock], [MinimumStock]) WHERE [IsActive] = 1;

CREATE UNIQUE INDEX [UX_Products_SKU] ON [dbo].[Products] ([SKU]);

CREATE INDEX [IX_PurchaseItems_ProductId] ON [dbo].[PurchaseItems] ([ProductId]);

CREATE INDEX [IX_PurchaseItems_PurchaseId] ON [dbo].[PurchaseItems] ([PurchaseId]);

CREATE INDEX [IX_Purchases_CreatedByUserId] ON [dbo].[Purchases] ([CreatedByUserId]);

CREATE INDEX [IX_Purchases_SupplierId_CreatedAtUtc] ON [dbo].[Purchases] ([SupplierId], [CreatedAtUtc]);

CREATE UNIQUE INDEX [UX_Purchases_PurchaseNumber] ON [dbo].[Purchases] ([PurchaseNumber]);

CREATE INDEX [IX_SaleItems_ProductId] ON [dbo].[SaleItems] ([ProductId]);

CREATE INDEX [IX_SaleItems_SaleId] ON [dbo].[SaleItems] ([SaleId]);

CREATE INDEX [IX_Sales_CreatedAtUtc] ON [dbo].[Sales] ([CreatedAtUtc]);

CREATE INDEX [IX_Sales_CreatedByUserId] ON [dbo].[Sales] ([CreatedByUserId]);

CREATE UNIQUE INDEX [UX_Sales_SaleNumber] ON [dbo].[Sales] ([SaleNumber]);

CREATE INDEX [IX_StockMovements_CreatedByUserId] ON [dbo].[StockMovements] ([CreatedByUserId]);

CREATE INDEX [IX_StockMovements_ProductId_CreatedAtUtc] ON [dbo].[StockMovements] ([ProductId], [CreatedAtUtc]);

CREATE INDEX [IX_StockMovements_Reference] ON [dbo].[StockMovements] ([ReferenceType], [ReferenceId]);

CREATE INDEX [IX_Suppliers_Name] ON [dbo].[Suppliers] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261125062523_InitialCreate', N'10.0.15');

COMMIT;
GO
