using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Transactions;

namespace MyFO.Infrastructure.Persistence.Configurations.Transactions;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("bank_accounts", "cfg");

        builder.HasKey(b => new { b.FamilyId, b.BankAccountId });

        builder.Property(b => b.FamilyId).HasColumnName("family_id");
        builder.Property(b => b.BankAccountId).HasColumnName("bank_account_id").ValueGeneratedOnAdd();
        builder.Property(b => b.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(b => b.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(b => b.Balance).HasColumnName("balance").HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(b => b.AccountNumber).HasColumnName("account_number").HasMaxLength(50);
        builder.Property(b => b.Cbu).HasColumnName("cbu").HasMaxLength(30);
        builder.Property(b => b.Alias).HasColumnName("alias").HasMaxLength(50);
        builder.Property(b => b.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.ModifiedAt).HasColumnName("modified_at");
        builder.Property(b => b.ModifiedBy).HasColumnName("modified_by");
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(b => b.IsDeleted);
        builder.Ignore(b => b.DomainEvents);

        builder.HasIndex(b => new { b.FamilyId, b.Name })
            .HasDatabaseName("ix_bank_accounts_family_name")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");
    }
}
