using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.Currencies.DTOs;

namespace MyFO.Application.Accounting.Currencies.Queries;

public record GetCurrenciesQuery : IRequest<List<CurrencyDto>>;
