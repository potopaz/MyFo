using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Transactions;

namespace MyFO.Infrastructure.Persistence.Configurations.Transactions;

public class FrequentMovementConfiguration : IEntityTypeConfiguration<FrequentMovement>
{
    public void Configure(EntityTypeBuilder<FrequentMovement> builder)
    {
        builder.ToTable("frequent_movements", "txn");

        builder.HasKey(f => new { f.FamilyId, f.FrequentMovementId });

        builder.Property(f => f.FamilyId).HasColumnName("family_id");
        builder.Property(f => f.FrequentMovementId).HasColumnName("frequent_movement_id").ValueGeneratedOnAdd();
        builder.Property(f => f.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(f => f.MovementType).HasColumnName("movement_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(f => f.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(f => f.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(f => f.SubcategoryId).HasColumnName("subcategory_id");
        builder.Property(f => f.AccountingType).HasColumnName("accounting_type").HasMaxLength(20);
        builder.Property(f => f.IsOrdinary).HasColumnName("is_ordinary");
        builder.Property(f => f.CostCenterId).HasColumnName("cost_center_id");

        builder.Property(f => f.PaymentMethodType).HasColumnName("payment_method_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.CashBoxId).HasColumnName("cash_box_id");
        builder.Property(f => f.BankAccountId).HasColumnName("bank_account_id");
        builder.Property(f => f.CreditCardId).HasColumnName("credit_card_id");
        builder.Property(f => f.CreditCardMemberId).HasColumnName("credit_card_member_id");

        builder.Property(f => f.FrequencyMonths).HasColumnName("frequency_months");
        builder.Property(f => f.LastAppliedAt).HasColumnName("last_applied_at");
        builder.Property(f => f.NextDueDate).HasColumnName("next_due_date");
        builder.Property(f => f.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(f => f.RowVersion).HasColumnName("row_version").HasDefaultValue(1);

        builder.Property(f => f.CreatedAt).HasColumnName("created_at");
        builder.Property(f => f.CreatedBy).HasColumnName("created_by");
        builder.Property(f => f.ModifiedAt).HasColumnName("modified_at");
        builder.Property(f => f.ModifiedBy).HasColumnName("modified_by");
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");
        builder.Property(f => f.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(f => f.IsDeleted);
        builder.Ignore(f => f.DomainEvents);

        builder.HasIndex(f => new { f.FamilyId, f.IsActive })
            .HasDatabaseName("ix_frequent_movements_family_active");
    }
}
