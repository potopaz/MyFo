namespace MyFO.Application.Auth.DTOs;

/// <summary>
/// Returned after a successful login or register.
/// Contains the JWT token and basic user info for the frontend.
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public List<UserFamilyDto> Families { get; set; } = [];
}

public class UserFamilyDto
{
    public Guid FamilyId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
