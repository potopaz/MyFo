using MediatR;
using MyFO.Application.Transactions.CashBoxes.DTOs;

namespace MyFO.Application.Transactions.CashBoxes.Queries;

public record GetCashBoxesQuery : IRequest<List<CashBoxDto>>;
