using MediatR;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public record DeleteFamilyCurrencyCommand(Guid FamilyCurrencyId) : IRequest;
