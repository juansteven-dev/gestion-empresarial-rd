using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Sales_Subtotal_NonNegative", "[Subtotal] >= 0");
            table.HasCheckConstraint("CK_Sales_Discount_NonNegative", "[Discount] >= 0");
            table.HasCheckConstraint("CK_Sales_Total_NonNegative", "[Total] >= 0");
            table.HasCheckConstraint("CK_Sales_Discount_NotGreaterThanSubtotal", "[Discount] <= [Subtotal]");
            table.HasCheckConstraint("CK_Sales_Total_MatchesSubtotalAndDiscount", "[Total] = [Subtotal] - [Discount]");
            table.HasCheckConstraint("CK_Sales_PaymentMethod_Valid", "[PaymentMethod] IN ('Cash', 'Card', 'Transfer')");
            table.HasCheckConstraint("CK_Sales_Status_Valid", "[Status] IN ('Completed', 'Cancelled')");
        });
        builder.HasKey(sale => sale.SaleId);
        builder.Property(sale => sale.SaleNumber).HasMaxLength(30).IsRequired();
        builder.Property(sale => sale.Status)
            .HasConversion<string>()
            .HasMaxLength(12)
            .IsUnicode(false)
            .HasDefaultValue(SaleStatus.Completed);
        builder.Property(sale => sale.Subtotal).HasPrecision(18, 2);
        builder.Property(sale => sale.Discount).HasPrecision(18, 2);
        builder.Property(sale => sale.Total).HasPrecision(18, 2);
        builder.Property(sale => sale.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsUnicode(false);
        builder.Property(sale => sale.Discount).HasDefaultValue(0m);
        builder.Property(sale => sale.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(sale => sale.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(sale => sale.SaleNumber).IsUnique().HasDatabaseName("UX_Sales_SaleNumber");
        builder.HasIndex(sale => sale.CreatedAtUtc).HasDatabaseName("IX_Sales_CreatedAtUtc");
        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(sale => sale.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
