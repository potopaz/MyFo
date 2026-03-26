namespace MyFO.Application.Auth.DTOs;

public class SelectFamilyRequest
{
    public Guid FamilyId { get; set; }
}

public class SelectFamilyResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
