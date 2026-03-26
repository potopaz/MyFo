using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Transactions;

namespace MyFO.Infrastructure.Persistence.Configurations.Transactions;

public class CashBoxConfiguration : IEntityTypeConfiguration<CashBox>
{
    public void Configure(EntityTypeBuilder<CashBox> builder)
    {
        builder.ToTable("cash_boxes", "cfg");

        builder.HasKey(c => new { c.FamilyId, c.CashBoxId });

        builder.Property(c => c.FamilyId).HasColumnName("family_id");
        builder.Property(c => c.CashBoxId).HasColumnName("cash_box_id").ValueGeneratedOnAdd();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(c => c.Balance).HasColumnName("balance").HasPrecision(18, 2).HasDefaultValue(0m);
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
            .HasDatabaseName("ix_cash_boxes_family_name")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
    }
}
