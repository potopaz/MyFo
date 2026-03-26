using MediatR;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public record CloseStatementPeriodCommand(Guid StatementPeriodId) : IRequest<StatementPeriodDto>;
