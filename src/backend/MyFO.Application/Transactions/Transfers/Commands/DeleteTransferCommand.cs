using MediatR;

namespace MyFO.Application.Transactions.Transfers.Commands;

public record DeleteTransferCommand(Guid TransferId) : IRequest;
