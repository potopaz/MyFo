using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Transactions;

namespace MyFO.Infrastructure.Persistence.Configurations.Transactions;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers", "txn");

        builder.HasKey(t => new { t.FamilyId, t.TransferId });

        builder.Property(t => t.FamilyId).HasColumnName("family_id");
        builder.Property(t => t.TransferId).HasColumnName("transfer_id").ValueGeneratedOnAdd();
        builder.Property(t => t.Date).HasColumnName("date");
        builder.Property(t => t.FromCashBoxId).HasColumnName("from_cash_box_id");
        builder.Property(t => t.FromBankAccountId).HasColumnName("from_bank_account_id");
        builder.Property(t => t.ToCashBoxId).HasColumnName("to_cash_box_id");
        builder.Property(t => t.ToBankAccountId).HasColumnName("to_bank_account_id");
        builder.Property(t => t.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(t => t.ExchangeRate).HasColumnName("exchange_rate").HasPrecision(18, 8).HasDefaultValue(1m);
        builder.Property(t => t.FromPrimaryExchangeRate).HasColumnName("from_primary_exchange_rate").HasPrecision(18, 8).HasDefaultValue(1m);
        builder.Property(t => t.FromSecondaryExchangeRate).HasColumnName("from_secondary_exchange_rate").HasPrecision(18, 8).HasDefaultValue(1m);
        builder.Property(t => t.ToPrimaryExchangeRate).HasColumnName("to_primary_exchange_rate").HasPrecision(18, 8).HasDefaultValue(1m);
        builder.Property(t => t.ToSecondaryExchangeRate).HasColumnName("to_secondary_exchange_rate").HasPrecision(18, 8).HasDefaultValue(1m);
        builder.Property(t => t.AmountTo).HasColumnName("amount_to").HasPrecision(18, 2);
        builder.Property(t => t.AmountToInPrimary).HasColumnName("amount_to_in_primary").HasPrecision(18, 2);
        builder.Property(t => t.AmountToInSecondary).HasColumnName("amount_to_in_secondary").HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(t => t.AmountInPrimary).HasColumnName("amount_in_primary").HasPrecision(18, 2);
        builder.Property(t => t.AmountInSecondary).HasColumnName("amount_in_secondary").HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(t => t.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(t => t.Source).HasColumnName("source");
        builder.Property(t => t.Status).HasColumnName("status");
        builder.Property(t => t.IsAutoConfirmed).HasColumnName("is_auto_confirmed");
        builder.Property(t => t.RejectionComment).HasColumnName("rejection_comment");
        builder.Property(t => t.RowVersion).HasColumnName("row_version").HasDefaultValue(1);

        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.ModifiedAt).HasColumnName("modified_at");
        builder.Property(t => t.ModifiedBy).HasColumnName("modified_by");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(t => t.IsDeleted);
        builder.Ignore(t => t.DomainEvents);

        // Navigation properties: no FK constraints enforced by EF (composite PKs, cross-family not needed)
        builder.HasOne(t => t.FromCashBox)
            .WithMany()
            .HasForeignKey(t => new { t.FamilyId, t.FromCashBoxId })
            .IsRequired(false);

        builder.HasOne(t => t.FromBankAccount)
            .WithMany()
            .HasForeignKey(t => new { t.FamilyId, t.FromBankAccountId })
            .IsRequired(false);

        builder.HasOne(t => t.ToCashBox)
            .WithMany()
            .HasForeignKey(t => new { t.FamilyId, t.ToCashBoxId })
            .IsRequired(false);

        builder.HasOne(t => t.ToBankAccount)
            .WithMany()
            .HasForeignKey(t => new { t.FamilyId, t.ToBankAccountId })
            .IsRequired(false);

        builder.HasIndex(t => new { t.FamilyId, t.Date })
            .HasDatabaseName("ix_transfers_family_date");
    }
}
