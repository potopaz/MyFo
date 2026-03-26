namespace MyFO.Infrastructure.Auth;

/// <summary>
/// JWT configuration read from appsettings.json.
/// Maps to the "Jwt" section.
/// </summary>
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 480; // 8 hours default
}
