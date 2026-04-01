using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.FrequentMovements.Commands;

public record ApplyFrequentMovementCommand(Guid FrequentMovementId, DateOnly MovementDate) : IRequest;
