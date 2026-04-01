using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.Movements.Commands;

public record DeleteMovementCommand(Guid MovementId) : IRequest;
