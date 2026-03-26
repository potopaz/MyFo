using MyFO.Domain.Common;

namespace MyFO.Domain.Identity;

/// <summary>
/// A one-time-use invitation token that allows a user to join a family.
/// Can be accepted by a new user (who registers) or an existing user (who logs in).
/// </summary>
public class FamilyInvitation : BaseEntity
{
    public Guid InvitationId { get; set; }
    public Guid FamilyId { get; set; }
    public string Token { get; set; } = string.Empty;

    /// <summary>Display name of the member who created the invitation (denormalized for public display).</summary>
    public string InvitedByDisplayName { get; set; } = string.Empty;

    /// <summary>Email address of the person being invited.</summary>
    public string InvitedEmail { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }

    // Navigation
    public Family Family { get; set; } = null!;
}
