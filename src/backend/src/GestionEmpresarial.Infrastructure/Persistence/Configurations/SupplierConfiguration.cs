using GestionEmpresarial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", "dbo", table =>
            table.HasCheckConstraint("CK_Suppliers_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0"));
        builder.HasKey(supplier => supplier.SupplierId);
        builder.Property(supplier => supplier.Name).HasMaxLength(120).IsRequired();
        builder.Property(supplier => supplier.TaxId).HasMaxLength(20);
        builder.Property(supplier => supplier.Phone).HasMaxLength(30);
        builder.Property(supplier => supplier.Email).HasMaxLength(254);
        builder.Property(supplier => supplier.Address).HasMaxLength(250);
        builder.Property(supplier => supplier.IsActive).HasDefaultValue(true);
        builder.Property(supplier => supplier.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(supplier => supplier.Name).HasDatabaseName("IX_Suppliers_Name");
        builder.HasIndex(supplier => supplier.Name).HasDatabaseName("IX_Suppliers_Name");
    }
}
