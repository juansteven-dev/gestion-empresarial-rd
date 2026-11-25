using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Purchases_Totals_NonNegative", "[Subtotal] >= 0 AND [Total] >= 0");
            table.HasCheckConstraint("CK_Purchases_Status_Valid", "[Status] IN ('Draft', 'Received', 'Cancelled')");
        });
        builder.HasKey(purchase => purchase.PurchaseId);
        builder.Property(purchase => purchase.PurchaseNumber).HasMaxLength(30).IsRequired();
        builder.Property(purchase => purchase.Status)
            .HasConversion<string>()
            .HasMaxLength(12)
            .IsUnicode(false)
            .HasDefaultValue(PurchaseStatus.Draft);
        builder.Property(purchase => purchase.Subtotal).HasPrecision(18, 2);
        builder.Property(purchase => purchase.Total).HasPrecision(18, 2);
        builder.Property(purchase => purchase.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(purchase => purchase.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(purchase => purchase.PurchaseNumber)
            .IsUnique()
            .HasDatabaseName("UX_Purchases_PurchaseNumber");
        builder.HasIndex(purchase => new { purchase.SupplierId, purchase.CreatedAtUtc })
            .HasDatabaseName("IX_Purchases_SupplierId_CreatedAtUtc");

        builder.HasOne(purchase => purchase.Supplier)
            .WithMany(supplier => supplier.Purchases)
            .HasForeignKey(purchase => purchase.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(purchase => purchase.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
