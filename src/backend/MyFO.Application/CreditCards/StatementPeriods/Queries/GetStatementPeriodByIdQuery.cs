using MediatR;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPeriods.Queries;

public record GetStatementPeriodByIdQuery(Guid StatementPeriodId) : IRequest<StatementPeriodDetailDto>;
