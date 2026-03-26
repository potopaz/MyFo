namespace MyFO.Application.Invitations.DTOs;

public class CreateInvitationResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
