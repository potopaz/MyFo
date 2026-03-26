using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Infrastructure.Services;

/// <summary>
/// Reads the current user's information from the JWT token in the HTTP request.
///
/// When a user logs in, they get a JWT that contains "claims" (key-value pairs):
///   - "sub" (subject) = the user's ID
///   - "family_id" = the currently selected family
///   - "is_super_admin" = whether they're a platform admin
///
/// This service extracts those claims so any code in the application can ask
/// "who is the current user?" without depending on HTTP directly.
///
/// Used by:
///   - AuditInterceptor (to fill CreatedBy/ModifiedBy)
///   - ApplicationDbContext (for tenant query filters)
///   - Command handlers (for authorization checks)
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return userId is not null ? Guid.Parse(userId) : Guid.Empty;
        }
    }

    public Guid? FamilyId
    {
        get
        {
            var familyId = _httpContextAccessor.HttpContext?.User
                .FindFirstValue("family_id");

            return familyId is not null ? Guid.Parse(familyId) : null;
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue("is_super_admin");

            return claim is not null && bool.Parse(claim);
        }
    }

    public bool IsFamilyAdmin
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.Role);

            return role == "FamilyAdmin";
        }
    }
}
