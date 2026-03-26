using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyFO.Domain.Common;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically fills audit fields on every save.
///
/// When you add a new entity:
///   - Sets CreatedAt to UTC now
///   - Sets CreatedBy to the current user's ID
///
/// When you modify an entity:
///   - Sets ModifiedAt to UTC now
///   - Sets ModifiedBy to the current user's ID
///
/// When you "delete" an entity (soft delete):
///   - Sets DeletedAt to UTC now
///   - Sets DeletedBy to the current user's ID
///
/// This means you NEVER have to manually set these fields in your handlers.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditFields(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditFields(DbContext context)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = userId;
                    break;
            }
        }
    }
}
