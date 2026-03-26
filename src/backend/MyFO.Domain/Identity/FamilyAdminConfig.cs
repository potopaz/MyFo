using MyFO.Domain.Common;

namespace MyFO.Domain.Identity;

public class FamilyAdminConfig : BaseEntity
{
    public Guid FamilyAdminConfigId { get; set; }
    public Guid FamilyId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? MaxMembers { get; set; }
    public string? Notes { get; set; }
    public DateTime? DisabledAt { get; set; }
    public string? DisabledReason { get; set; }

    // Navigation
    public Family Family { get; set; } = null!;
}
