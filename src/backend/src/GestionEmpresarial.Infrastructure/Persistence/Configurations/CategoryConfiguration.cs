using GestionEmpresarial.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpresarial.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "dbo", table =>
            table.HasCheckConstraint("CK_Categories_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0"));
        builder.HasKey(category => category.CategoryId);
        builder.Property(category => category.Name).HasMaxLength(80).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(250);
        builder.Property(category => category.IsActive).HasDefaultValue(true);
        builder.Property(category => category.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(category => category.Name).IsUnique().HasDatabaseName("UX_Categories_Name");
    }
}
