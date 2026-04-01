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

    private record FamilyCount(Guid FamilyId, int Count);

    public async Task<Dictionary<Guid, int>> GetSubcategoryCountsByFamilyAsync(CancellationToken cancellationToken) =>
        await Database
            .SqlQueryRaw<FamilyCount>(@"SELECT family_id AS ""FamilyId"", CAST(COUNT(*) AS integer) AS ""Count"" FROM cfg.subcategories WHERE deleted_at IS NULL GROUP BY family_id")
            .ToDictionaryAsync(r => r.FamilyId, r => r.Count, cancellationToken);

    public async Task<Dictionary<Guid, int>> GetCostCenterCountsByFamilyAsync(CancellationToken cancellationToken) =>
        await Database
            .SqlQueryRaw<FamilyCount>(@"SELECT family_id AS ""FamilyId"", CAST(COUNT(*) AS integer) AS ""Count"" FROM cfg.cost_centers WHERE deleted_at IS NULL GROUP BY family_id")
            .ToDictionaryAsync(r => r.FamilyId, r => r.Count, cancellationToken);

    public async Task<Dictionary<Guid, int>> GetMovementCountsByFamilyAsync(CancellationToken cancellationToken) =>
        await Database
            .SqlQueryRaw<FamilyCount>(@"SELECT family_id AS ""FamilyId"", CAST(COUNT(*) AS integer) AS ""Count"" FROM txn.movements WHERE deleted_at IS NULL GROUP BY family_id")
            .ToDictionaryAsync(r => r.FamilyId, r => r.Count, cancellationToken);

    public async Task<Dictionary<Guid, int>> GetTransferCountsByFamilyAsync(CancellationToken cancellationToken) =>
        await Database
            .SqlQueryRaw<FamilyCount>(@"SELECT family_id AS ""FamilyId"", CAST(COUNT(*) AS integer) AS ""Count"" FROM txn.transfers WHERE deleted_at IS NULL GROUP BY family_id")
            .ToDictionaryAsync(r => r.FamilyId, r => r.Count, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FamilyConfiguration());
        modelBuilder.ApplyConfiguration(new FamilyMemberConfiguration());
        modelBuilder.ApplyConfiguration(new FamilyAdminConfigConfiguration());
    }
}
