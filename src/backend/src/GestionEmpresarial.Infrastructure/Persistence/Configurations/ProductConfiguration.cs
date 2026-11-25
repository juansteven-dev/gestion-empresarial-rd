using GestionEmpresarial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Products_CostPrice_NonNegative", "[CostPrice] >= 0");
            table.HasCheckConstraint("CK_Products_SalePrice_NonNegative", "[SalePrice] >= 0");
            table.HasCheckConstraint("CK_Products_CurrentStock_NonNegative", "[CurrentStock] >= 0");
            table.HasCheckConstraint("CK_Products_MinimumStock_NonNegative", "[MinimumStock] >= 0");
            table.HasCheckConstraint("CK_Products_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });
        builder.HasKey(product => product.ProductId);
        builder.Property(product => product.SKU).HasMaxLength(40).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(140).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(500);
        builder.Property(product => product.CostPrice).HasPrecision(18, 2);
        builder.Property(product => product.SalePrice).HasPrecision(18, 2);
        builder.Property(product => product.CurrentStock).HasDefaultValue(0);
        builder.Property(product => product.MinimumStock).HasDefaultValue(0);
        builder.Property(product => product.IsActive).HasDefaultValue(true);
        builder.Property(product => product.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(product => product.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(product => product.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(product => product.SKU).IsUnique().HasDatabaseName("UX_Products_SKU");
        builder.HasIndex(product => product.CategoryId).HasDatabaseName("IX_Products_CategoryId");
        builder.HasIndex(product => product.Name).HasDatabaseName("IX_Products_Name");
        builder.HasIndex(product => new { product.CurrentStock, product.MinimumStock })
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_Products_Stock_MinimumStock_Active");

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
