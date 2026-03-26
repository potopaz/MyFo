using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyFO.Application.Auth;

namespace MyFO.Infrastructure.Auth;

public class VerificationTokenService : IVerificationTokenService
{
    private readonly JwtSettings _jwtSettings;

    public VerificationTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateRegistrationToken(string email, string fullName, string passwordHash, string language)
    {
        var claims = new List<Claim>
        {
            new("purpose", "registration"),
            new("email", email),
            new("full_name", fullName),
            new("password_hash", passwordHash),
            new("language", language)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RegistrationTokenData? ValidateRegistrationToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = key
            }, out _);

            var purpose = principal.FindFirstValue("purpose");
            if (purpose != "registration")
                return null;

            return new RegistrationTokenData
            {
                Email = principal.FindFirstValue("email") ?? string.Empty,
                FullName = principal.FindFirstValue("full_name") ?? string.Empty,
                PasswordHash = principal.FindFirstValue("password_hash") ?? string.Empty,
                Language = principal.FindFirstValue("language") ?? "es"
            };
        }
        catch
        {
            return null;
        }
    }
}
