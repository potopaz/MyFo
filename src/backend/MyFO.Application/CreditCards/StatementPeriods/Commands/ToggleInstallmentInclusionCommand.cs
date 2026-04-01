using MyFO.Application.Common.Mediator;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class ToggleInstallmentInclusionCommand : IRequest
{
    public Guid StatementPeriodId { get; set; }
    public Guid CreditCardInstallmentId { get; set; }
    public bool Include { get; set; }
}
