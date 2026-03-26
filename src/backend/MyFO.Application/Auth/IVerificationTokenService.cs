namespace MyFO.Application.Auth;

public interface IVerificationTokenService
{
    string GenerateRegistrationToken(string email, string fullName, string passwordHash, string language);
    RegistrationTokenData? ValidateRegistrationToken(string token);
}

public class RegistrationTokenData
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
}
