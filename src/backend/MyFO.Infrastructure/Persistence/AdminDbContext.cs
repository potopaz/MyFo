using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Identity;
using MyFO.Infrastructure.Persistence.Configurations.Identity;

namespace MyFO.Infrastructure.Persistence;

/// <summary>
/// Separate DbContext for SuperAdmin operations.
/// Uses DefaultConnection (postgres superuser) — bypasses PostgreSQL RLS.
/// No tenant interceptors, no audit interceptors.
/// </summary>
public class AdminDbContext : DbContext, IAdminDbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<FamilyAdminConfig> FamilyAdminConfigs => Set<FamilyAdminConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FamilyConfiguration());
        modelBuilder.ApplyConfiguration(new FamilyMemberConfiguration());
        modelBuilder.ApplyConfiguration(new FamilyAdminConfigConfiguration());
    }
}
