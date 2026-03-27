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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
