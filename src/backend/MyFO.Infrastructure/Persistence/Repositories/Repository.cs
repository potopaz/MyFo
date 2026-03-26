using Microsoft.EntityFrameworkCore;
using MyFO.Domain.Common;
using MyFO.Domain.Interfaces.Repositories;

namespace MyFO.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
///
/// This is intentionally simple — it delegates filtering to EF Core's
/// Global Query Filters (soft delete + tenant isolation are automatic).
///
/// You don't need to write WHERE deleted_at IS NULL or WHERE tenant_id = X
/// anywhere. The DbContext handles that for every query.
///
/// For entities with composite PKs (TenantEntity), GetByIdAsync uses
/// the entity-specific ID. The tenant filter is applied automatically.
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public void SoftDelete(T entity)
    {
        // We don't physically delete — just set DeletedAt.
        // The AuditInterceptor will fill DeletedAt and DeletedBy automatically
        // when it detects the entity state is Modified and DeletedAt has a value.
        entity.DeletedAt = DateTime.UtcNow;
        DbSet.Update(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }
}
