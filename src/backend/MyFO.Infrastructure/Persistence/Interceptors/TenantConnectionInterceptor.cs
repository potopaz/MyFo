using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that sets PostgreSQL session variables every time
/// a connection is opened. This is the bridge between JWT and RLS:
///
///   JWT → CurrentUserService → This interceptor → PostgreSQL session vars → RLS policies
///
/// Two variables are set:
///   - app.current_family_id: used by tenant_isolation policies on all tenant tables
///   - app.current_user_id: used by the user_membership_lookup policy on family_members
///     (needed for login, when family_id is not yet known)
///
/// If neither value is available (e.g., unauthenticated request), nothing is set
/// and RLS blocks all tenant rows — which is the correct behavior.
/// </summary>
public class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public TenantConnectionInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetSessionVariables(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        SetSessionVariables(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void SetSessionVariables(DbConnection connection)
    {
        var parts = new List<string>();

        if (_currentUser.FamilyId is not null)
            parts.Add($"SET app.current_family_id = '{_currentUser.FamilyId.Value}'");

        if (_currentUser.UserId != Guid.Empty)
            parts.Add($"SET app.current_user_id = '{_currentUser.UserId}'");

        if (parts.Count == 0)
            return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = string.Join("; ", parts);
        cmd.ExecuteNonQuery();
    }
}
