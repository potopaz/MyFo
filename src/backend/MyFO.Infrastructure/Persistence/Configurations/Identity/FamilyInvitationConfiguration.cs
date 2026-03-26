using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Identity;

namespace MyFO.Infrastructure.Persistence.Configurations.Identity;

public class FamilyInvitationConfiguration : IEntityTypeConfiguration<FamilyInvitation>
{
    public void Configure(EntityTypeBuilder<FamilyInvitation> builder)
    {
        builder.ToTable("family_invitations", "cfg");

        builder.HasKey(i => i.InvitationId);

        builder.Property(i => i.InvitationId)
            .HasColumnName("invitation_id")
            .ValueGeneratedOnAdd();

        builder.Property(i => i.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(i => i.Token)
            .HasColumnName("token")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(i => i.InvitedByDisplayName)
            .HasColumnName("invited_by_display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.InvitedEmail)
            .HasColumnName("invited_email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(i => i.AcceptedAt)
            .HasColumnName("accepted_at");

        builder.Property(i => i.AcceptedByUserId)
            .HasColumnName("accepted_by_user_id");

        // Audit fields
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.CreatedBy).HasColumnName("created_by");
        builder.Property(i => i.ModifiedAt).HasColumnName("modified_at");
        builder.Property(i => i.ModifiedBy).HasColumnName("modified_by");
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.Property(i => i.DeletedBy).HasColumnName("deleted_by");

        builder.Ignore(i => i.IsDeleted);
        builder.Ignore(i => i.DomainEvents);

        builder.HasIndex(i => i.Token)
            .IsUnique()
            .HasDatabaseName("ix_family_invitations_token");

        builder.HasIndex(i => i.FamilyId)
            .HasDatabaseName("ix_family_invitations_family_id");

        builder.HasIndex(i => new { i.FamilyId, i.InvitedEmail })
            .HasDatabaseName("ix_family_invitations_family_email");

        // No navigation to Family to avoid accidental global query filter issues
        builder.Ignore(i => i.Family);
    }
}
