using MyFO.Application.Common.Mediator;
using MyFO.Application.Identity.FamilySettings.DTOs;

namespace MyFO.Application.Identity.FamilySettings.Commands;

public class UpdateFamilySettingsCommand : IRequest<FamilySettingsDto>
{
    public string Name { get; set; } = string.Empty;
    public string PrimaryCurrencyCode { get; set; } = string.Empty;
    public string SecondaryCurrencyCode { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
}
