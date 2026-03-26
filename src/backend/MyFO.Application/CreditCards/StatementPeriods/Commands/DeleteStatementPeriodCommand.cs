using MediatR;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public record DeleteStatementPeriodCommand(Guid StatementPeriodId) : IRequest;
