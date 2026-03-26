using MediatR;
using MyFO.Application.Transactions.Transfers.DTOs;

namespace MyFO.Application.Transactions.Transfers.Queries;

public record GetTransfersQuery(DateOnly? DateFrom, DateOnly? DateTo, string? Status = null) : IRequest<List<TransferListItemDto>>;
