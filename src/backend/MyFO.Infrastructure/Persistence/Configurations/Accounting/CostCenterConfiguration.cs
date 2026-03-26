using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Accounting;

namespace MyFO.Infrastructure.Persistence.Configurations.Accounting;

public class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable("cost_centers", "cfg");

        builder.HasKey(c => new { c.FamilyId, c.CostCenterId });

        builder.Property(c => c.FamilyId).HasColumnName("family_id");
        builder.Property(c => c.CostCenterId).HasColumnName("cost_center_id").ValueGeneratedOnAdd();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.ModifiedAt).HasColumnName("modified_at");
        builder.Property(c => c.ModifiedBy).HasColumnName("modified_by");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(c => c.IsDeleted);
        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => new { c.FamilyId, c.Name })
            .HasDatabaseName("ix_cost_centers_family_name")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
    }
}
