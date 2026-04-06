using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;

namespace MyFO.Infrastructure.Persistence.Configurations.CreditCards;

public class StatementPeriodConfiguration : IEntityTypeConfiguration<StatementPeriod>
{
    public void Configure(EntityTypeBuilder<StatementPeriod> builder)
    {
        builder.ToTable("statement_periods", "txn");

        builder.HasKey(s => new { s.FamilyId, s.StatementPeriodId });

        builder.Property(s => s.FamilyId).HasColumnName("family_id");
        builder.Property(s => s.StatementPeriodId).HasColumnName("statement_period_id").ValueGeneratedOnAdd();
        builder.Property(s => s.CreditCardId).HasColumnName("credit_card_id").IsRequired();
        builder.Property(s => s.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(s => s.DueDate).HasColumnName("due_date").IsRequired();
        builder.Property(s => s.PaymentStatus).HasColumnName("payment_status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.PreviousBalance).HasColumnName("previous_balance").HasPrecision(18, 2);
        builder.Property(s => s.InstallmentsTotal).HasColumnName("installments_total").HasPrecision(18, 2);
        builder.Property(s => s.ChargesTotal).HasColumnName("charges_total").HasPrecision(18, 2);
        builder.Property(s => s.BonificationsTotal).HasColumnName("bonifications_total").HasPrecision(18, 2);
        builder.Property(s => s.StatementTotal).HasColumnName("statement_total").HasPrecision(18, 2);
        builder.Property(s => s.PaymentsTotal).HasColumnName("payments_total").HasPrecision(18, 2);
        builder.Property(s => s.PendingBalance).HasColumnName("pending_balance").HasPrecision(18, 2);
        builder.Property(s => s.ClosedAt).HasColumnName("closed_at");
        builder.Property(s => s.ClosedBy).HasColumnName("closed_by");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.ModifiedAt).HasColumnName("modified_at");
        builder.Property(s => s.ModifiedBy).HasColumnName("modified_by");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(s => s.IsDeleted);
        builder.Ignore(s => s.DomainEvents);

        builder.HasIndex(s => new { s.FamilyId, s.CreditCardId })
            .HasDatabaseName("ix_statement_periods_card");

        // Only one open period per card
        builder.HasIndex(s => new { s.FamilyId, s.CreditCardId })
            .HasDatabaseName("ix_statement_periods_one_open")
            .IsUnique()
            .HasFilter("closed_at IS NULL AND deleted_at IS NULL");

        builder.HasMany(s => s.Installments)
            .WithOne(i => i.StatementPeriod)
            .HasForeignKey(i => new { i.FamilyId, i.StatementPeriodId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.LineItems)
            .WithOne(li => li.StatementPeriod)
            .HasForeignKey(li => new { li.FamilyId, li.StatementPeriodId })
            .OnDelete(DeleteBehavior.Restrict);

        // Payments relation is configured in CreditCardPaymentConfiguration
    }
}
