namespace MyFO.Application.Auth.DTOs;

/// <summary>
/// Data sent by the client to register a new user.
/// The user also creates their first family in the same request.
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // First family setup
    public string FamilyName { get; set; } = string.Empty;
    public string PrimaryCurrencyCode { get; set; } = string.Empty;
    public string SecondaryCurrencyCode { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
}
