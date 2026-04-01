using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.Transfers.DTOs;

namespace MyFO.Application.Transactions.Transfers.Queries;

public record GetTransferByIdQuery(Guid TransferId) : IRequest<TransferDto>;
