namespace MyFO.Application.Admin.DTOs;

public class AdminFamilyListItemDto
{
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public bool IsEnabled { get; set; }
    public int? MaxMembers { get; set; }
    public string? Notes { get; set; }
    public DateTime? DisabledAt { get; set; }
    public string? DisabledReason { get; set; }
    public int SubcategoryCount { get; set; }
    public int CostCenterCount { get; set; }
    public int MovementCount { get; set; }
    public int TransferCount { get; set; }
}
