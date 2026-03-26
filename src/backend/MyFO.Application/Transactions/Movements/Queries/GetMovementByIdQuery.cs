using MediatR;
using MyFO.Application.Transactions.Movements.DTOs;

namespace MyFO.Application.Transactions.Movements.Queries;

public record GetMovementByIdQuery(Guid MovementId) : IRequest<MovementDto>;
