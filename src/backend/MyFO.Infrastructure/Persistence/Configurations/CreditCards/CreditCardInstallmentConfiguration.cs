using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards;

namespace MyFO.Infrastructure.Persistence.Configurations.CreditCards;

public class CreditCardInstallmentConfiguration : IEntityTypeConfiguration<CreditCardInstallment>
{
    public void Configure(EntityTypeBuilder<CreditCardInstallment> builder)
    {
        builder.ToTable("credit_card_installments", "txn");

        builder.HasKey(i => new { i.FamilyId, i.CreditCardInstallmentId });

        builder.Property(i => i.FamilyId).HasColumnName("family_id");
        builder.Property(i => i.CreditCardInstallmentId).HasColumnName("credit_card_installment_id").ValueGeneratedOnAdd();
        builder.Property(i => i.MovementPaymentId).HasColumnName("movement_payment_id").IsRequired();
        builder.Property(i => i.InstallmentNumber).HasColumnName("installment_number").IsRequired();
        builder.Property(i => i.ProjectedAmount).HasColumnName("projected_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.BonificationApplied).HasColumnName("bonification_applied").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.EffectiveAmount).HasColumnName("effective_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.ActualAmount).HasColumnName("actual_amount").HasPrecision(18, 2);
        builder.Property(i => i.ActualBonificationAmount).HasColumnName("actual_bonification_amount").HasPrecision(18, 2);
        builder.Property(i => i.EstimatedDate).HasColumnName("estimated_date").IsRequired();
        builder.Property(i => i.StatementPeriodId).HasColumnName("statement_period_id");

        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.CreatedBy).HasColumnName("created_by");
        builder.Property(i => i.ModifiedAt).HasColumnName("modified_at");
        builder.Property(i => i.ModifiedBy).HasColumnName("modified_by");
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.Property(i => i.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(i => i.IsDeleted);
        builder.Ignore(i => i.DomainEvents);

        builder.HasIndex(i => new { i.FamilyId, i.MovementPaymentId })
            .HasDatabaseName("ix_cc_installments_payment");

        builder.HasIndex(i => new { i.FamilyId, i.StatementPeriodId })
            .HasDatabaseName("ix_cc_installments_period")
            .HasFilter("statement_period_id IS NOT NULL");
    }
}
