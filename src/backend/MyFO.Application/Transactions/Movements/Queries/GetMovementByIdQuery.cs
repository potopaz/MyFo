using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.Movements.DTOs;

namespace MyFO.Application.Transactions.Movements.Queries;

public record GetMovementByIdQuery(Guid MovementId) : IRequest<MovementDto>;
