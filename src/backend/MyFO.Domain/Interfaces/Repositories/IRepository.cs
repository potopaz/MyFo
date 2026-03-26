using MyFO.Domain.Common;

namespace MyFO.Domain.Interfaces.Repositories;

/// <summary>
/// Generic repository interface for basic CRUD operations.
/// Implemented in Infrastructure using Entity Framework Core.
/// Soft delete and tenant filtering are handled automatically by EF Core query filters.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void SoftDelete(T entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
