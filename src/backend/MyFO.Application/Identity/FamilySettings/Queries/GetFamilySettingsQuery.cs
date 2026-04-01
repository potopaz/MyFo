using MyFO.Application.Common.Mediator;
using MyFO.Application.Identity.FamilySettings.DTOs;

namespace MyFO.Application.Identity.FamilySettings.Queries;

public record GetFamilySettingsQuery : IRequest<FamilySettingsDto>;
