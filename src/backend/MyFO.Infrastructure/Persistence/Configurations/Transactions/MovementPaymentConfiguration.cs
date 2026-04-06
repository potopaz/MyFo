using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Transactions;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Infrastructure.Persistence.Configurations.Transactions;

public class MovementPaymentConfiguration : IEntityTypeConfiguration<MovementPayment>
{
    public void Configure(EntityTypeBuilder<MovementPayment> builder)
    {
        builder.ToTable("movement_payments", "txn");

        builder.HasKey(p => new { p.FamilyId, p.MovementPaymentId });

        builder.Property(p => p.FamilyId).HasColumnName("family_id");
        builder.Property(p => p.MovementPaymentId).HasColumnName("movement_payment_id").ValueGeneratedOnAdd();
        builder.Property(p => p.MovementId).HasColumnName("movement_id");
        builder.Property(p => p.PaymentMethodType).HasColumnName("payment_method_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(p => p.CashBoxId).HasColumnName("cash_box_id");
        builder.Property(p => p.BankAccountId).HasColumnName("bank_account_id");
        builder.Property(p => p.CreditCardId).HasColumnName("credit_card_id");
        builder.Property(p => p.CreditCardMemberId).HasColumnName("credit_card_member_id");
        builder.Property(p => p.Installments).HasColumnName("installments");
        builder.Property(p => p.IsReconciled).HasColumnName("is_reconciled").HasDefaultValue(false);

        // Credit card bonification fields
        builder.Property(p => p.BonificationType).HasColumnName("bonification_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.BonificationValue).HasColumnName("bonification_value").HasPrecision(18, 2);
        builder.Property(p => p.BonificationAmount).HasColumnName("bonification_amount").HasPrecision(18, 2);
        builder.Property(p => p.NetAmount).HasColumnName("net_amount").HasPrecision(18, 2);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.ModifiedAt).HasColumnName("modified_at");
        builder.Property(p => p.ModifiedBy).HasColumnName("modified_by");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(p => p.IsDeleted);
        builder.Ignore(p => p.DomainEvents);

        builder.HasIndex(p => new { p.FamilyId, p.MovementId })
            .HasDatabaseName("ix_movement_payments_family_movement");

        builder.HasMany(p => p.CreditCardInstallments)
            .WithOne()
            .HasForeignKey(i => new { i.FamilyId, i.MovementPaymentId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
