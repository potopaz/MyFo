namespace MyFO.Domain.Interfaces.Services;

/// <summary>
/// Provides information about the currently authenticated user and their active family.
/// Implemented in Infrastructure by reading JWT claims from the HTTP context.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid? FamilyId { get; }
    bool IsSuperAdmin { get; }
    bool IsFamilyAdmin { get; }
}
