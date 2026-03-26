using MediatR;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementLineItems.Commands;

public class AddStatementLineItemCommand : IRequest<StatementLineItemDto>
{
    public Guid StatementPeriodId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
