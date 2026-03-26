using MediatR;

namespace MyFO.Application.Transactions.FrequentMovements.Commands;

public record DeleteFrequentMovementCommand(Guid FrequentMovementId) : IRequest;
