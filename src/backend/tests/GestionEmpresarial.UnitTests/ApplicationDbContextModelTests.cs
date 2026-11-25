using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GestionEmpresarial.UnitTests;

public sealed class ApplicationDbContextModelTests
{
    [Fact]
    public void Model_ContainsIdentityAndDocumentedDomainEntities()
    {
        using var context = CreateContext();

        var expectedEntities = new[]
        {
            typeof(IdentityUser),
            typeof(Category),
            typeof(Product),
            typeof(Supplier),
            typeof(Purchase),
            typeof(PurchaseItem),
            typeof(Sale),
            typeof(SaleItem),
            typeof(StockMovement),
            typeof(AuditLog)
        };

        foreach (var entityType in expectedEntities)
        {
            Assert.NotNull(context.Model.FindEntityType(entityType));
        }
    }

    [Fact]
    public void Model_UsesDecimal18Scale2ForEveryMoneyProperty()
    {
        using var context = CreateContext();
        var moneyProperties = new (Type Entity, string Property)[]
        {
            (typeof(Product), nameof(Product.CostPrice)),
            (typeof(Product), nameof(Product.SalePrice)),
            (typeof(Purchase), nameof(Purchase.Subtotal)),
            (typeof(Purchase), nameof(Purchase.Total)),
            (typeof(PurchaseItem), nameof(PurchaseItem.UnitCost)),
            (typeof(PurchaseItem), nameof(PurchaseItem.LineTotal)),
            (typeof(Sale), nameof(Sale.Subtotal)),
            (typeof(Sale), nameof(Sale.Discount)),
            (typeof(Sale), nameof(Sale.Total)),
            (typeof(SaleItem), nameof(SaleItem.UnitPrice)),
            (typeof(SaleItem), nameof(SaleItem.LineTotal))
        };

        foreach (var (entity, property) in moneyProperties)
        {
            var modelProperty = context.Model.FindEntityType(entity)!.FindProperty(property)!;
            Assert.Equal(18, modelProperty.GetPrecision());
            Assert.Equal(2, modelProperty.GetScale());
        }
    }

    [Fact]
    public void Model_EnforcesUniqueBusinessKeysAndRestrictedHistoryRelationships()
    {
        using var context = CreateContext();

        AssertUniqueIndex<Category>(context, nameof(Category.Name));
        AssertUniqueIndex<Product>(context, nameof(Product.SKU));
        AssertUniqueIndex<Purchase>(context, nameof(Purchase.PurchaseNumber));
        AssertUniqueIndex<Sale>(context, nameof(Sale.SaleNumber));
        var supplierNameIndex = context.Model.FindEntityType(typeof(Supplier))!.GetIndexes()
            .Single(index => index.Properties.Count == 1 && index.Properties[0].Name == nameof(Supplier.Name));
        Assert.False(supplierNameIndex.IsUnique);
        AssertIdentityForeignKeyIsRestricted<Purchase>(context);
        AssertIdentityForeignKeyIsRestricted<Sale>(context);
        AssertIdentityForeignKeyIsRestricted<StockMovement>(context);
        AssertIdentityForeignKeyIsRestricted<AuditLog>(context);
    }

    [Fact]
    public void Model_MapsUtcDatesAndRejectsNonUtcValues()
    {
        using var context = CreateContext();
        var dateProperties = new (Type Entity, string Property)[]
        {
            (typeof(Category), "CreatedAtUtc"),
            (typeof(Product), "CreatedAtUtc"),
            (typeof(Product), "UpdatedAtUtc"),
            (typeof(Supplier), "CreatedAtUtc"),
            (typeof(Purchase), "CreatedAtUtc"),
            (typeof(Purchase), "ReceivedAtUtc"),
            (typeof(Sale), "CreatedAtUtc"),
            (typeof(StockMovement), "CreatedAtUtc"),
            (typeof(AuditLog), "CreatedAtUtc")
        };

        foreach (var (entity, propertyName) in dateProperties)
        {
            var property = context.Model.FindEntityType(entity)!.FindProperty(propertyName)!;
            Assert.Equal("datetime2", property.GetColumnType());

            var converter = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter>(
                property.GetValueConverter());
            Assert.Throws<InvalidOperationException>(() => converter.ConvertToProvider(DateTime.Now));
            var materialized = (DateTime)converter.ConvertFromProvider(DateTime.SpecifyKind(
                new DateTime(2026, 1, 1), DateTimeKind.Unspecified))!;
            Assert.Equal(DateTimeKind.Utc, materialized.Kind);
        }
    }

    [Fact]
    public void Model_RejectsNegativeStockAndRequiresAdjustmentReasons()
    {
        using var context = CreateContext();

        var designModel = context.GetService<IDesignTimeModel>().Model;
        var productChecks = designModel.FindEntityType(typeof(Product))!.GetCheckConstraints();
        var movementChecks = designModel.FindEntityType(typeof(StockMovement))!.GetCheckConstraints();
        var saleChecks = designModel.FindEntityType(typeof(Sale))!.GetCheckConstraints();
        var auditChecks = designModel.FindEntityType(typeof(AuditLog))!.GetCheckConstraints();

        Assert.Contains(productChecks, check => check.Name == "CK_Products_CurrentStock_NonNegative");
        Assert.Contains(movementChecks, check => check.Name == "CK_StockMovements_Adjustment_RequiresNotes");
        Assert.Contains(movementChecks, check => check.Name == "CK_StockMovements_Balance_MatchesDelta");
        Assert.Contains(movementChecks, check => check.Name == "CK_StockMovements_ReferenceType_Valid");
        Assert.Contains(saleChecks, check => check.Name == "CK_Sales_Total_MatchesSubtotalAndDiscount");
        Assert.Contains(saleChecks, check => check.Name == "CK_Sales_PaymentMethod_Valid");
        Assert.Contains(saleChecks, check => check.Name == "CK_Sales_Status_Valid");
        Assert.Contains(auditChecks, check => check.Name == "CK_AuditLogs_DetailsJson_Valid");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnlyTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void AssertUniqueIndex<TEntity>(ApplicationDbContext context, string propertyName)
    {
        var index = context.Model.FindEntityType(typeof(TEntity))!.GetIndexes()
            .Single(index => index.Properties.Count == 1 && index.Properties[0].Name == propertyName);
        Assert.True(index.IsUnique);
    }

    private static void AssertIdentityForeignKeyIsRestricted<TEntity>(ApplicationDbContext context)
    {
        var foreignKey = context.Model.FindEntityType(typeof(TEntity))!.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(IdentityUser));
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}
