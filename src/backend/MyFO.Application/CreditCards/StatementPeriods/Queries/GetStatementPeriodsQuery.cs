using MediatR;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPeriods.Queries;

public record GetStatementPeriodsQuery(Guid CreditCardId) : IRequest<List<StatementPeriodDto>>;
