using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFO.Domain.Accounting;

namespace MyFO.Infrastructure.Persistence.Configurations.Accounting;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "cfg");

        builder.HasKey(c => new { c.FamilyId, c.CategoryId });

        builder.Property(c => c.FamilyId).HasColumnName("family_id");
        builder.Property(c => c.CategoryId).HasColumnName("category_id").ValueGeneratedOnAdd();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Icon).HasColumnName("icon").HasMaxLength(50);

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.CreatedBy).HasColumnName("created_by");
        builder.Property(c => c.ModifiedAt).HasColumnName("modified_at");
        builder.Property(c => c.ModifiedBy).HasColumnName("modified_by");
        builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        builder.Property(c => c.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(c => c.IsDeleted);
        builder.Ignore(c => c.DomainEvents);

        // Unique name per family
        builder.HasIndex(c => new { c.FamilyId, c.Name })
            .HasDatabaseName("ix_categories_family_name")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasMany(c => c.Subcategories)
            .WithOne(s => s.Category)
            .HasForeignKey(s => new { s.FamilyId, s.CategoryId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
