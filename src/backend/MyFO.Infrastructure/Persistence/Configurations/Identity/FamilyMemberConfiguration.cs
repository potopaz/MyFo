using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Identity;
using MyFO.Domain.Identity.Enums;

namespace MyFO.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for the FamilyMember entity.
///
/// FamilyMember links an ApplicationUser (login) to a Family (tenant).
/// One user can be a member of multiple families.
///
/// Uses COMPOSITE primary key: (family_id, member_id).
/// This is a key architectural decision — every tenant-scoped entity
/// uses (family_id, entity_id) as its PK for RLS compatibility.
/// </summary>
public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.ToTable("family_members", "cfg");

        // Composite primary key: (family_id, member_id)
        builder.HasKey(m => new { m.FamilyId, m.MemberId });

        builder.Property(m => m.FamilyId)
            .HasColumnName("family_id");

        builder.Property(m => m.MemberId)
            .HasColumnName("member_id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()    // Store as "Member" / "FamilyAdmin" instead of 0 / 1
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        // Audit fields
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.ModifiedAt).HasColumnName("modified_at");
        builder.Property(m => m.ModifiedBy).HasColumnName("modified_by");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        builder.Property(m => m.DeletedBy).HasColumnName("deleted_by");

        // Ignore computed property and domain events
        builder.Ignore(m => m.IsDeleted);
        builder.Ignore(m => m.DomainEvents);

        // Index for fast lookup: "given a user, find all their families"
        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("ix_family_members_user_id");
    }
}
