using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards;

namespace MyFO.Infrastructure.Persistence.Configurations.CreditCards;

public class CreditCardPaymentConfiguration : IEntityTypeConfiguration<CreditCardPayment>
{
    public void Configure(EntityTypeBuilder<CreditCardPayment> builder)
    {
        builder.ToTable("credit_card_payments", "txn");

        builder.HasKey(p => new { p.FamilyId, p.CreditCardPaymentId });

        builder.Property(p => p.FamilyId).HasColumnName("family_id");
        builder.Property(p => p.CreditCardPaymentId).HasColumnName("credit_card_payment_id").ValueGeneratedOnAdd();
        builder.Property(p => p.CreditCardId).HasColumnName("credit_card_id").IsRequired();
        builder.Property(p => p.PaymentDate).HasColumnName("payment_date").IsRequired();
        builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(200);
        builder.Property(p => p.CashBoxId).HasColumnName("cash_box_id");
        builder.Property(p => p.BankAccountId).HasColumnName("bank_account_id");
        builder.Property(p => p.IsTotalPayment).HasColumnName("is_total_payment").IsRequired();
        builder.Property(p => p.StatementPeriodId).HasColumnName("statement_period_id");
        builder.Property(p => p.PrimaryExchangeRate).HasColumnName("primary_exchange_rate").HasPrecision(18, 6).IsRequired();
        builder.Property(p => p.SecondaryExchangeRate).HasColumnName("secondary_exchange_rate").HasPrecision(18, 6).IsRequired();
        builder.Property(p => p.AmountInPrimary).HasColumnName("amount_in_primary").HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.AmountInSecondary).HasColumnName("amount_in_secondary").HasPrecision(18, 2).IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.ModifiedAt).HasColumnName("modified_at");
        builder.Property(p => p.ModifiedBy).HasColumnName("modified_by");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(p => p.IsDeleted);
        builder.Ignore(p => p.DomainEvents);

        builder.HasIndex(p => new { p.FamilyId, p.CreditCardId })
            .HasDatabaseName("ix_credit_card_payments_card");

        builder.HasIndex(p => new { p.FamilyId, p.StatementPeriodId })
            .HasDatabaseName("ix_credit_card_payments_period")
            .HasFilter("statement_period_id IS NOT NULL");

        builder.HasOne(p => p.CreditCard)
            .WithMany()
            .HasForeignKey(p => new { p.FamilyId, p.CreditCardId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.StatementPeriod)
            .WithMany(sp => sp.Payments)
            .HasForeignKey(p => new { p.FamilyId, p.StatementPeriodId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Allocations)
            .WithOne(a => a.CreditCardPayment)
            .HasForeignKey(a => new { a.FamilyId, a.CreditCardPaymentId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
