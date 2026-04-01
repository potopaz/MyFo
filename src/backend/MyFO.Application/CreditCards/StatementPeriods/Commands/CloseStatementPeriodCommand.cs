using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public record CloseStatementPeriodCommand(Guid StatementPeriodId) : IRequest<StatementPeriodDto>;
