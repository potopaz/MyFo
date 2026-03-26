using MediatR;
using MyFO.Application.Transactions.FrequentMovements.DTOs;

namespace MyFO.Application.Transactions.FrequentMovements.Queries;

public record GetFrequentMovementByIdQuery(Guid FrequentMovementId) : IRequest<FrequentMovementDto>;
