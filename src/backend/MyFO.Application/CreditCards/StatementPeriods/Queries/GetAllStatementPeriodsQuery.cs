using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPeriods.Queries;

public record GetAllStatementPeriodsQuery(Guid? CreditCardId, string? Status) : IRequest<List<StatementPeriodDto>>;
