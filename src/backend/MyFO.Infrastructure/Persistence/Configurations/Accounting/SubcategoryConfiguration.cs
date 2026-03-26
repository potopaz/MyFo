using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Accounting;

namespace MyFO.Infrastructure.Persistence.Configurations.Accounting;

public class SubcategoryConfiguration : IEntityTypeConfiguration<Subcategory>
{
    public void Configure(EntityTypeBuilder<Subcategory> builder)
    {
        builder.ToTable("subcategories", "cfg");

        builder.HasKey(s => new { s.FamilyId, s.SubcategoryId });

        builder.Property(s => s.FamilyId).HasColumnName("family_id");
        builder.Property(s => s.SubcategoryId).HasColumnName("subcategory_id").ValueGeneratedOnAdd();
        builder.Property(s => s.CategoryId).HasColumnName("category_id").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.SubcategoryType).HasColumnName("subcategory_type").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(s => s.SuggestedAccountingType).HasColumnName("suggested_accounting_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.SuggestedCostCenterId).HasColumnName("suggested_cost_center_id");
        builder.Property(s => s.IsOrdinary).HasColumnName("is_ordinary");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by");
        builder.Property(s => s.ModifiedAt).HasColumnName("modified_at");
        builder.Property(s => s.ModifiedBy).HasColumnName("modified_by");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");
        builder.Property(s => s.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(s => s.IsDeleted);
        builder.Ignore(s => s.DomainEvents);

        // Unique name within same category
        builder.HasIndex(s => new { s.FamilyId, s.CategoryId, s.Name })
            .HasDatabaseName("ix_subcategories_family_category_name")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasOne(s => s.SuggestedCostCenter)
            .WithMany()
            .HasForeignKey(s => new { s.FamilyId, s.SuggestedCostCenterId })
            .OnDelete(DeleteBehavior.SetNull);
    }
}
