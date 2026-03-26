using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Accounting;

namespace MyFO.Infrastructure.Persistence.Configurations.Accounting;

/// <summary>
/// Global currency table — NOT tenant-scoped.
/// Managed by system admin, seeded with common currencies.
/// </summary>
public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies", "cmn");

        builder.HasKey(c => c.CurrencyId);

        builder.Property(c => c.CurrencyId).HasColumnName("currency_id").ValueGeneratedOnAdd();
        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(3).IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(c => c.Symbol).HasColumnName("symbol").HasMaxLength(5).IsRequired();
        builder.Property(c => c.DecimalPlaces).HasColumnName("decimal_places").HasDefaultValue(2);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.ModifiedAt).HasColumnName("modified_at");
        builder.Property(c => c.ModifiedBy).HasColumnName("modified_by");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(c => c.IsDeleted);
        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => c.Code)
            .HasDatabaseName("ix_currencies_code")
            .IsUnique();
    }
}
