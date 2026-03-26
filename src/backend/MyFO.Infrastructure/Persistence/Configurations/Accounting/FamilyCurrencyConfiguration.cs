using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Accounting;

namespace MyFO.Infrastructure.Persistence.Configurations.Accounting;

public class FamilyCurrencyConfiguration : IEntityTypeConfiguration<FamilyCurrency>
{
    public void Configure(EntityTypeBuilder<FamilyCurrency> builder)
    {
        builder.ToTable("family_currencies", "cfg");

        builder.HasKey(fc => new { fc.FamilyId, fc.FamilyCurrencyId });

        builder.Property(fc => fc.FamilyId).HasColumnName("family_id");
        builder.Property(fc => fc.FamilyCurrencyId).HasColumnName("family_currency_id").ValueGeneratedOnAdd();
        builder.Property(fc => fc.CurrencyId).HasColumnName("currency_id").IsRequired();
        builder.Property(fc => fc.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(fc => fc.CreatedAt).HasColumnName("created_at");
        builder.Property(fc => fc.CreatedBy).HasColumnName("created_by");
        builder.Property(fc => fc.ModifiedAt).HasColumnName("modified_at");
        builder.Property(fc => fc.ModifiedBy).HasColumnName("modified_by");
        builder.Property(fc => fc.DeletedAt).HasColumnName("deleted_at");
        builder.Property(fc => fc.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(fc => fc.IsDeleted);
        builder.Ignore(fc => fc.DomainEvents);

        // One currency per family (can't add ARS twice)
        builder.HasIndex(fc => new { fc.FamilyId, fc.CurrencyId })
            .HasDatabaseName("ix_family_currencies_family_currency")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(fc => fc.Currency)
            .WithMany()
            .HasForeignKey(fc => fc.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
