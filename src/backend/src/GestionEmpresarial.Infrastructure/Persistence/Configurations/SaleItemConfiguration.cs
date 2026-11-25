using GestionEmpresarial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems", "dbo", table =>
        {
            table.HasCheckConstraint("CK_SaleItems_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_SaleItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_SaleItems_LineTotal_NonNegative", "[LineTotal] >= 0");
            table.HasCheckConstraint("CK_SaleItems_ProductName_NotEmpty", "LEN(LTRIM(RTRIM([ProductName]))) > 0");
            table.HasCheckConstraint("CK_SaleItems_SKU_NotEmpty", "LEN(LTRIM(RTRIM([SKU]))) > 0");
        });
        builder.HasKey(item => item.SaleItemId);
        builder.Property(item => item.ProductName).HasMaxLength(140).IsRequired();
        builder.Property(item => item.SKU).HasMaxLength(40).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.LineTotal).HasPrecision(18, 2);
        builder.HasIndex(item => item.ProductId).HasDatabaseName("IX_SaleItems_ProductId");
        builder.HasOne(item => item.Sale)
            .WithMany(sale => sale.Items)
            .HasForeignKey(item => item.SaleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Product)
            .WithMany(product => product.SaleItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
