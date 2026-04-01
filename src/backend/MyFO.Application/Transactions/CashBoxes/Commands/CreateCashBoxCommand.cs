using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.CashBoxes.DTOs;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public record CreateCashBoxCommand(
    string Name,
    string CurrencyCode,
    decimal InitialBalance = 0
) : IRequest<CashBoxDto>;
