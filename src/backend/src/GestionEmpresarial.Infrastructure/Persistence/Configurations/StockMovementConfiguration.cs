using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", "dbo", table =>
        {
            table.HasCheckConstraint("CK_StockMovements_QuantityDelta_NonZero", "[QuantityDelta] <> 0");
            table.HasCheckConstraint("CK_StockMovements_StockBefore_NonNegative", "[StockBefore] >= 0");
            table.HasCheckConstraint("CK_StockMovements_StockAfter_NonNegative", "[StockAfter] >= 0");
            table.HasCheckConstraint("CK_StockMovements_Balance_MatchesDelta", "[StockAfter] = [StockBefore] + [QuantityDelta]");
            table.HasCheckConstraint("CK_StockMovements_Type_Valid", "[MovementType] IN ('Purchase', 'Sale', 'Adjustment')");
            table.HasCheckConstraint("CK_StockMovements_ReferenceType_Valid", "[ReferenceType] IN ('Purchase', 'Sale', 'Manual')");
            table.HasCheckConstraint("CK_StockMovements_Adjustment_RequiresNotes", "[MovementType] <> 'Adjustment' OR LEN(LTRIM(RTRIM(COALESCE([Notes], N'')))) > 0");
        });
        builder.HasKey(movement => movement.StockMovementId);
        builder.Property(movement => movement.MovementType)
            .HasConversion<string>()
            .HasMaxLength(12)
            .IsUnicode(false);
        builder.Property(movement => movement.ReferenceType)
            .HasConversion<string>()
            .HasMaxLength(12)
            .IsUnicode(false);
        builder.Property(movement => movement.Notes).HasMaxLength(250);
        builder.Property(movement => movement.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(movement => movement.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(movement => new { movement.ProductId, movement.CreatedAtUtc })
            .HasDatabaseName("IX_StockMovements_ProductId_CreatedAtUtc");
        builder.HasIndex(movement => new { movement.ReferenceType, movement.ReferenceId })
            .HasDatabaseName("IX_StockMovements_Reference");
        builder.HasOne(movement => movement.Product)
            .WithMany(product => product.StockMovements)
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(movement => movement.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
