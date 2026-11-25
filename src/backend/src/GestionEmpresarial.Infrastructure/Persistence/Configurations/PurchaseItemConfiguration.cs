using GestionEmpresarial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems", "dbo", table =>
        {
            table.HasCheckConstraint("CK_PurchaseItems_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_PurchaseItems_UnitCost_NonNegative", "[UnitCost] >= 0");
            table.HasCheckConstraint("CK_PurchaseItems_LineTotal_NonNegative", "[LineTotal] >= 0");
        });
        builder.HasKey(item => item.PurchaseItemId);
        builder.Property(item => item.UnitCost).HasPrecision(18, 2);
        builder.Property(item => item.LineTotal).HasPrecision(18, 2);
        builder.HasIndex(item => item.ProductId).HasDatabaseName("IX_PurchaseItems_ProductId");
        builder.HasOne(item => item.Purchase)
            .WithMany(purchase => purchase.Items)
            .HasForeignKey(item => item.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Product)
            .WithMany(product => product.PurchaseItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
