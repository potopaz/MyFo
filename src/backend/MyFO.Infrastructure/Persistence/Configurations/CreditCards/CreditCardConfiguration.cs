using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards;

namespace MyFO.Infrastructure.Persistence.Configurations.CreditCards;

public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards", "cfg");

        builder.HasKey(c => new { c.FamilyId, c.CreditCardId });

        builder.Property(c => c.FamilyId).HasColumnName("family_id");
        builder.Property(c => c.CreditCardId).HasColumnName("credit_card_id").ValueGeneratedOnAdd();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
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
            .HasDatabaseName("ix_credit_cards_family_name")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasMany(c => c.Members)
            .WithOne(m => m.CreditCard)
            .HasForeignKey(m => new { m.FamilyId, m.CreditCardId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.StatementPeriods)
            .WithOne(s => s.CreditCard)
            .HasForeignKey(s => new { s.FamilyId, s.CreditCardId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
