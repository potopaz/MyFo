using MyFO.Domain.Common;
using MyFO.Domain.Identity.Enums;

namespace MyFO.Domain.Identity;

/// <summary>
/// Links a user (from ASP.NET Identity) to a family with a specific role.
/// A user can be a member of multiple families.
/// A family can have multiple members.
/// </summary>
public class FamilyMember : TenantEntity
{
    public Guid MemberId { get; set; }
    public Guid UserId { get; set; }              // References the ASP.NET Identity user
    public UserRole Role { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Family Family { get; set; } = null!;
}
