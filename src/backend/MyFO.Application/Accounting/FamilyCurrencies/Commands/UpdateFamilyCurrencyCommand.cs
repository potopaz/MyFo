using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public record UpdateFamilyCurrencyCommand(Guid FamilyCurrencyId, bool IsActive) : IRequest<FamilyCurrencyDto>;
