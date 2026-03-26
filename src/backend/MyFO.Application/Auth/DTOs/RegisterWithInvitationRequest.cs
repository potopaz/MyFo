namespace MyFO.Application.Auth.DTOs;

public class RegisterWithInvitationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string InvitationToken { get; set; } = string.Empty;
}
