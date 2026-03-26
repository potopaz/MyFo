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
        builder.Property(a => a.StatementLineItemId).HasColumnName("statement_line_item_id");
        builder.Property(a => a.AmountCardCurrency).HasColumnName("amount_card_currency").HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.AmountInPrimary).HasColumnName("amount_in_primary").HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.AmountInSecondary).HasColumnName("amount_in_secondary").HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.PrimaryExchangeRate).HasColumnName("primary_exchange_rate").HasPrecision(18, 6).IsRequired();
        builder.Property(a => a.SecondaryExchangeRate).HasColumnName("secondary_exchange_rate").HasPrecision(18, 6).IsRequired();

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by");
        builder.Property(a => a.ModifiedAt).HasColumnName("modified_at");
        builder.Property(a => a.ModifiedBy).HasColumnName("modified_by");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        builder.Property(a => a.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(a => a.IsDeleted);
        builder.Ignore(a => a.DomainEvents);

        builder.HasIndex(a => new { a.FamilyId, a.CreditCardPaymentId })
            .HasDatabaseName("ix_allocations_payment");

        builder.HasIndex(a => new { a.FamilyId, a.CreditCardInstallmentId })
            .HasDatabaseName("ix_allocations_installment")
            .HasFilter("credit_card_installment_id IS NOT NULL");

        builder.HasIndex(a => new { a.FamilyId, a.StatementLineItemId })
            .HasDatabaseName("ix_allocations_line_item")
            .HasFilter("statement_line_item_id IS NOT NULL");
    }
}
