using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.CreditCards;

namespace MyFO.Infrastructure.Persistence.Configurations.CreditCards;

public class StatementLineItemConfiguration : IEntityTypeConfiguration<StatementLineItem>
{
    public void Configure(EntityTypeBuilder<StatementLineItem> builder)
    {
        builder.ToTable("statement_line_items", "txn");

        builder.HasKey(li => new { li.FamilyId, li.StatementLineItemId });

        builder.Property(li => li.FamilyId).HasColumnName("family_id");
        builder.Property(li => li.StatementLineItemId).HasColumnName("statement_line_item_id").ValueGeneratedOnAdd();
        builder.Property(li => li.StatementPeriodId).HasColumnName("statement_period_id").IsRequired();
        builder.Property(li => li.LineType).HasColumnName("line_type")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(li => li.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(li => li.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();

        builder.Property(li => li.CreatedAt).HasColumnName("created_at");
        builder.Property(li => li.CreatedBy).HasColumnName("created_by");
        builder.Property(li => li.ModifiedAt).HasColumnName("modified_at");
        builder.Property(li => li.ModifiedBy).HasColumnName("modified_by");
        builder.Property(li => li.DeletedAt).HasColumnName("deleted_at");
        builder.Property(li => li.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(li => li.IsDeleted);
        builder.Ignore(li => li.DomainEvents);

        builder.HasIndex(li => new { li.FamilyId, li.StatementPeriodId })
            .HasDatabaseName("ix_statement_line_items_period");
    }
}
