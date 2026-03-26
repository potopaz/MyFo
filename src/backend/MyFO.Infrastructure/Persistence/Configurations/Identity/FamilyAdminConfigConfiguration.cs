using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Identity;

namespace MyFO.Infrastructure.Persistence.Configurations.Identity;

public class FamilyAdminConfigConfiguration : IEntityTypeConfiguration<FamilyAdminConfig>
{
    public void Configure(EntityTypeBuilder<FamilyAdminConfig> builder)
    {
        builder.ToTable("family_admin_configs", "cfg");

        builder.HasKey(c => c.FamilyAdminConfigId);
        builder.Property(c => c.FamilyAdminConfigId)
            .HasColumnName("family_admin_config_id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(c => c.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(c => c.MaxMembers)
            .HasColumnName("max_members");

        builder.Property(c => c.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(c => c.DisabledAt)
            .HasColumnName("disabled_at");

        builder.Property(c => c.DisabledReason)
            .HasColumnName("disabled_reason")
            .HasMaxLength(200);

        // Audit fields
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.ModifiedAt).HasColumnName("modified_at");
        builder.Property(c => c.ModifiedBy).HasColumnName("modified_by");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");

        builder.Ignore(c => c.IsDeleted);
        builder.Ignore(c => c.DomainEvents);

        builder.HasOne(c => c.Family)
            .WithMany()
            .HasForeignKey(c => c.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.FamilyId).IsUnique();
    }
}
