using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;

namespace MyFO.Application.Accounting.FamilyCurrencies.Queries;

public record GetFamilyCurrenciesQuery : IRequest<List<FamilyCurrencyDto>>;
