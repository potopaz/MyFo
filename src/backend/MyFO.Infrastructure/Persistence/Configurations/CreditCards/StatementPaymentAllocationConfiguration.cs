using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards;

namespace MyFO.Infrastructure.Persistence.Configurations.CreditCards;

public class StatementPaymentAllocationConfiguration : IEntityTypeConfiguration<StatementPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<StatementPaymentAllocation> builder)
    {
        builder.ToTable("statement_payment_allocations", "txn");

        builder.HasKey(a => new { a.FamilyId, a.AllocationId });

        builder.Property(a => a.FamilyId).HasColumnName("family_id");
        builder.Property(a => a.AllocationId).HasColumnName("allocation_id").ValueGeneratedOnAdd();
        builder.Property(a => a.CreditCardPaymentId).HasColumnName("credit_card_payment_id").IsRequired();
        builder.Property(a => a.CreditCardInstallmentId).HasColumnName("credit_card_installment_id");
        builder.Property(a => a.MovementPaymentId).HasColumnName("movement_payment_id");
        builder.Property(a => a.StatementLineItemId).HasColumnName("statement_line_item_id");
        builder.Property(a => a.AmountCardCurrency).HasColumnName("amount_card_currency").HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.AmountInPrimary).HasColumnName("amount_in_primary").HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.AmountInSecondary).HasColumnName("amount_in_secondary").HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.PrimaryExchangeRate).HasColumnName("primary_exchange_rate").HasPrecision(18, 8).IsRequired();
        builder.Property(a => a.SecondaryExchangeRate).HasColumnName("secondary_exchange_rate").HasPrecision(18, 8).IsRequired();

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.ModifiedAt).HasColumnName("modified_at");
        builder.Property(a => a.ModifiedBy).HasColumnName("modified_by");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(a => a.IsDeleted);
        builder.Ignore(a => a.DomainEvents);

        // CreditCardPayment FK is defined from the other side in CreditCardPaymentConfiguration
        // (HasMany(Allocations).WithOne(CreditCardPayment)) — do NOT redeclare here to avoid shadow properties

        builder.HasOne(a => a.CreditCardInstallment)
            .WithMany()
            .HasForeignKey(a => new { a.FamilyId, a.CreditCardInstallmentId })
            .IsRequired(false);

        builder.HasOne(a => a.MovementPayment)
            .WithMany()
            .HasForeignKey(a => new { a.FamilyId, a.MovementPaymentId })
            .IsRequired(false);

        builder.HasOne(a => a.StatementLineItem)
            .WithMany()
            .HasForeignKey(a => new { a.FamilyId, a.StatementLineItemId })
            .IsRequired(false);

        builder.HasIndex(a => new { a.FamilyId, a.CreditCardPaymentId })
            .HasDatabaseName("ix_statement_payment_allocations_payment");
    }
}
