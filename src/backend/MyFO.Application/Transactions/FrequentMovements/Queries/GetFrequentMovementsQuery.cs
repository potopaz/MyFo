using MediatR;
using MyFO.Application.Transactions.FrequentMovements.DTOs;

namespace MyFO.Application.Transactions.FrequentMovements.Queries;

public record GetFrequentMovementsQuery : IRequest<List<FrequentMovementListItemDto>>;
