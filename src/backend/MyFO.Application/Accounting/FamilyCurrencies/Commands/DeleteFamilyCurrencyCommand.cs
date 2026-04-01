using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public record DeleteFamilyCurrencyCommand(Guid FamilyCurrencyId) : IRequest;
