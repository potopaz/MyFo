using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.Transfers.Commands;

public record ConfirmTransferCommand(Guid TransferId) : IRequest;
