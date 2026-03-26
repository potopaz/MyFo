using Microsoft.AspNetCore.Identity;

namespace MyFO.Infrastructure.Identity;

/// <summary>
/// Extends ASP.NET Identity's IdentityUser with application-specific fields.
///
/// This is the "real" user that logs in. It's different from FamilyMember,
/// which represents the relationship between a user and a family.
///
/// One ApplicationUser can be a FamilyMember of multiple families.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public string Language { get; set; } = "es";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
