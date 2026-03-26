using MediatR;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public record DeleteCashBoxCommand(Guid CashBoxId) : IRequest;
