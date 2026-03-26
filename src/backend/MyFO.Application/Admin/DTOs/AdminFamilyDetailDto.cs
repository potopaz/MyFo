namespace MyFO.Application.Admin.DTOs;

public class AdminFamilyDetailDto : AdminFamilyListItemDto
{
    public string PrimaryCurrencyCode { get; set; } = string.Empty;
    public string SecondaryCurrencyCode { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<AdminFamilyMemberDto> Members { get; set; } = [];
}
