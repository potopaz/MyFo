using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Identity;

namespace MyFO.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for the Family entity.
///
/// This tells EF Core exactly how to create the "families" table:
/// - Table name in snake_case
/// - Primary key is family_id (NOT the generic "Id")
/// - Which columns are required, their max lengths, etc.
///
/// Family is the TENANT. It does NOT inherit TenantEntity because
/// a family doesn't belong to another family — it IS the tenant.
/// </summary>
public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("families", "cfg");

        // Primary key
        builder.HasKey(f => f.FamilyId);
        builder.Property(f => f.FamilyId)
            .HasColumnName("family_id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(f => f.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.PrimaryCurrencyCode)
            .HasColumnName("primary_currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(f => f.SecondaryCurrencyCode)
            .HasColumnName("secondary_currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(f => f.Language)
            .HasColumnName("language")
            .HasMaxLength(5)
            .HasDefaultValue("es")
            .IsRequired();

        // Audit fields (snake_case column names)
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.CreatedBy).HasColumnName("created_by");
        builder.Property(f => f.ModifiedAt).HasColumnName("modified_at");
        builder.Property(f => f.ModifiedBy).HasColumnName("modified_by");
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");
        builder.Property(f => f.DeletedBy).HasColumnName("deleted_by");

        // Ignore computed property (not a column)
        builder.Ignore(f => f.IsDeleted);

        // Ignore domain events (not persisted)
        builder.Ignore(f => f.DomainEvents);

        // Relationships
        builder.HasMany(f => f.Members)
            .WithOne(m => m.Family)
            .HasForeignKey(m => m.FamilyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
