using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public record ReopenStatementPeriodCommand(Guid StatementPeriodId) : IRequest<StatementPeriodDto>;
