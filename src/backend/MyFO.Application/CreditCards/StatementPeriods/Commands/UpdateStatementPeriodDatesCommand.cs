using MediatR;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class UpdateStatementPeriodDatesCommand : IRequest
{
    public Guid StatementPeriodId { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
}
