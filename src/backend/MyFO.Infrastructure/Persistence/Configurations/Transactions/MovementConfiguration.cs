using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Transactions;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Infrastructure.Persistence.Configurations.Transactions;

public class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> builder)
    {
        builder.ToTable("movements", "txn");

        builder.HasKey(m => new { m.FamilyId, m.MovementId });

        builder.Property(m => m.FamilyId).HasColumnName("family_id");
        builder.Property(m => m.MovementId).HasColumnName("movement_id").ValueGeneratedOnAdd();
        builder.Property(m => m.Date).HasColumnName("date");
        builder.Property(m => m.MovementType).HasColumnName("movement_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(m => m.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(m => m.PrimaryExchangeRate).HasColumnName("primary_exchange_rate").HasPrecision(18, 8).HasDefaultValue(1m);
        builder.Property(m => m.SecondaryExchangeRate).HasColumnName("secondary_exchange_rate").HasPrecision(18, 8).HasDefaultValue(1m);
        builder.Property(m => m.AmountInPrimary).HasColumnName("amount_in_primary").HasPrecision(18, 2);
        builder.Property(m => m.AmountInSecondary).HasColumnName("amount_in_secondary").HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(m => m.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(m => m.SubcategoryId).HasColumnName("subcategory_id");
        builder.Property(m => m.AccountingType).HasColumnName("accounting_type").HasMaxLength(20);
        builder.Property(m => m.IsOrdinary).HasColumnName("is_ordinary");
        builder.Property(m => m.CostCenterId).HasColumnName("cost_center_id");

        builder.Property(m => m.Source).HasColumnName("source").HasMaxLength(20);
        builder.Property(m => m.RowVersion).HasColumnName("row_version").HasDefaultValue(1);

        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.ModifiedAt).HasColumnName("modified_at");
        builder.Property(m => m.ModifiedBy).HasColumnName("modified_by");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        builder.Property(m => m.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(m => m.IsDeleted);
        builder.Ignore(m => m.DomainEvents);

        builder.HasMany(m => m.Payments)
            .WithOne(p => p.Movement)
            .HasForeignKey(p => new { p.FamilyId, p.MovementId });

        builder.HasIndex(m => new { m.FamilyId, m.Date })
            .HasDatabaseName("ix_movements_family_date");
    }
}
