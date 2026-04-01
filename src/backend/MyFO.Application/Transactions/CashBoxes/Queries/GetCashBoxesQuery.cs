using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.CashBoxes.DTOs;

namespace MyFO.Application.Transactions.CashBoxes.Queries;

public record GetCashBoxesQuery(bool IncludeAll = false) : IRequest<List<CashBoxDto>>;
