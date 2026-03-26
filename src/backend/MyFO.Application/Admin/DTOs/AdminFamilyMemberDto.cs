namespace MyFO.Application.Admin.DTOs;

public class AdminFamilyMemberDto
{
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
