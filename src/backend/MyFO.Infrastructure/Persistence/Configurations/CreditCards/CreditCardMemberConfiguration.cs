using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards;

namespace MyFO.Infrastructure.Persistence.Configurations.CreditCards;

public class CreditCardMemberConfiguration : IEntityTypeConfiguration<CreditCardMember>
{
    public void Configure(EntityTypeBuilder<CreditCardMember> builder)
    {
        builder.ToTable("credit_card_members", "cfg");

        builder.HasKey(m => new { m.FamilyId, m.CreditCardMemberId });

        builder.Property(m => m.FamilyId).HasColumnName("family_id");
        builder.Property(m => m.CreditCardMemberId).HasColumnName("credit_card_member_id").ValueGeneratedOnAdd();
        builder.Property(m => m.CreditCardId).HasColumnName("credit_card_id").IsRequired();
        builder.Property(m => m.HolderName).HasColumnName("holder_name").HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastFourDigits).HasColumnName("last_four_digits").HasMaxLength(4);
        builder.Property(m => m.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(m => m.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(m => m.ExpirationMonth).HasColumnName("expiration_month");
        builder.Property(m => m.ExpirationYear).HasColumnName("expiration_year");

        // Only one active primary member per card
        builder.HasIndex(m => new { m.FamilyId, m.CreditCardId })
            .IsUnique()
            .HasFilter("is_primary = true AND deleted_at IS NULL")
            .HasDatabaseName("ix_credit_card_members_one_primary");

        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.ModifiedAt).HasColumnName("modified_at");
        builder.Property(m => m.ModifiedBy).HasColumnName("modified_by");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        builder.Property(m => m.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(m => m.IsDeleted);
        builder.Ignore(m => m.DomainEvents);
    }
}
