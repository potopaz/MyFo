using Microsoft.EntityFrameworkCore;
using MyFO.Domain.Identity;

namespace MyFO.Application.Common.Interfaces;

/// <summary>
/// DbContext for SuperAdmin queries. Uses DefaultConnection (postgres superuser)
/// which bypasses PostgreSQL RLS, allowing cross-tenant data access.
/// Only inject this in admin handlers — never in tenant-scoped handlers.
/// </summary>
public interface IAdminDbContext
{
    DbSet<Family> Families { get; }
    DbSet<FamilyMember> FamilyMembers { get; }
    DbSet<FamilyAdminConfig> FamilyAdminConfigs { get; }

    Task<Dictionary<Guid, int>> GetSubcategoryCountsByFamilyAsync(CancellationToken cancellationToken);
    Task<Dictionary<Guid, int>> GetCostCenterCountsByFamilyAsync(CancellationToken cancellationToken);
    Task<Dictionary<Guid, int>> GetMovementCountsByFamilyAsync(CancellationToken cancellationToken);
    Task<Dictionary<Guid, int>> GetTransferCountsByFamilyAsync(CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
