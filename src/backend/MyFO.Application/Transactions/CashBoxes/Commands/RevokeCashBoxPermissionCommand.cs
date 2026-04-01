using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public record RevokeCashBoxPermissionCommand(Guid CashBoxId, Guid MemberId) : IRequest;
