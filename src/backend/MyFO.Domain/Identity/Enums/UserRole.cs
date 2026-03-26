namespace MyFO.Domain.Identity.Enums;

/// <summary>
/// Role of a user within a specific family (tenant).
/// SuperAdmin is handled at the Identity/platform level, not here.
/// </summary>
public enum UserRole
{
    Member = 0,
    FamilyAdmin = 1
}
