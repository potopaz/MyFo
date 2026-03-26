using MediatR;

namespace MyFO.Application.Transactions.Transfers.Commands;

public record ConfirmTransferCommand(Guid TransferId) : IRequest;
