namespace MyFO.Application.Auth.DTOs;

public class InitiateRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
}
