namespace MyFO.Application.Auth.DTOs;

public class CreateFamilyRequest
{
    public string Name { get; set; } = string.Empty;
    public string PrimaryCurrencyCode { get; set; } = "ARS";
    public string SecondaryCurrencyCode { get; set; } = "USD";
}
