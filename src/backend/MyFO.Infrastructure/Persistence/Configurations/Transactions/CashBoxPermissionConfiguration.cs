using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Transactions;

namespace MyFO.Infrastructure.Persistence.Configurations.Transactions;

public class CashBoxPermissionConfiguration : IEntityTypeConfiguration<CashBoxPermission>
{
    public void Configure(EntityTypeBuilder<CashBoxPermission> builder)
    {
        builder.ToTable("cash_box_permissions", "cfg");

        builder.HasKey(e => new { e.FamilyId, e.CashBoxId, e.MemberId });

        builder.Property(e => e.FamilyId).HasColumnName("family_id");
        builder.Property(e => e.CashBoxId).HasColumnName("cash_box_id");
        builder.Property(e => e.MemberId).HasColumnName("member_id");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.ModifiedAt).HasColumnName("modified_at");
        builder.Property(e => e.ModifiedBy).HasColumnName("modified_by");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(e => e.IsDeleted);
        builder.Ignore(e => e.DomainEvents);
    }
}
