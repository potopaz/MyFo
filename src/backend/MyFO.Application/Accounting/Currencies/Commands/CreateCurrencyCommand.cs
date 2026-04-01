using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.Currencies.DTOs;

namespace MyFO.Application.Accounting.Currencies.Commands;

public record CreateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces = 2
) : IRequest<CurrencyDto>;
