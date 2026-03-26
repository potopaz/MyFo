namespace MyFO.Application.Invitations.DTOs;

public class InvitationInfoDto
{
    public string FamilyName { get; set; } = string.Empty;
    public string InvitedByDisplayName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
}
