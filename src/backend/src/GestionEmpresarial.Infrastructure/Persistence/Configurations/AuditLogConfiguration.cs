using GestionEmpresarial.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "dbo", table =>
        {
            table.HasCheckConstraint("CK_AuditLogs_Action_NotEmpty", "LEN(LTRIM(RTRIM([Action]))) > 0");
            table.HasCheckConstraint("CK_AuditLogs_EntityName_NotEmpty", "LEN(LTRIM(RTRIM([EntityName]))) > 0");
            table.HasCheckConstraint("CK_AuditLogs_DetailsJson_Valid", "[DetailsJson] IS NULL OR ISJSON([DetailsJson]) = 1");
        });
        builder.HasKey(auditLog => auditLog.AuditLogId);
        builder.Property(auditLog => auditLog.UserId).HasMaxLength(450).IsRequired();
        builder.Property(auditLog => auditLog.Action).HasMaxLength(80).IsRequired();
        builder.Property(auditLog => auditLog.EntityName).HasMaxLength(80).IsRequired();
        builder.Property(auditLog => auditLog.EntityId).HasMaxLength(80);
        builder.Property(auditLog => auditLog.DetailsJson).HasColumnType("nvarchar(max)");
        builder.Property(auditLog => auditLog.IpAddress).HasMaxLength(45);
        builder.Property(auditLog => auditLog.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(auditLog => auditLog.CreatedAtUtc).HasDatabaseName("IX_AuditLogs_CreatedAtUtc");
        builder.HasIndex(auditLog => new { auditLog.UserId, auditLog.CreatedAtUtc })
            .HasDatabaseName("IX_AuditLogs_UserId_CreatedAtUtc");
        builder.HasOne<IdentityUser>()
            .WithMany()
            .HasForeignKey(auditLog => auditLog.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
