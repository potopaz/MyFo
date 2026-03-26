namespace MyFO.Application.Identity.FamilySettings.DTOs;

public class FamilySettingsDto
{
    public string Name { get; set; } = string.Empty;
    public string PrimaryCurrencyCode { get; set; } = string.Empty;
    public string SecondaryCurrencyCode { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
    public bool CanChangeCurrencies { get; set; } = true;
}
