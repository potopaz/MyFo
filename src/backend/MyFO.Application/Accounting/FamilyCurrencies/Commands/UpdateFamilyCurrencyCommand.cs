using MediatR;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public record UpdateFamilyCurrencyCommand(Guid FamilyCurrencyId, bool IsActive) : IRequest<FamilyCurrencyDto>;
