using MediatR;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class ToggleBonificationInclusionCommand : IRequest
{
    public Guid StatementPeriodId { get; set; }
    public Guid CreditCardInstallmentId { get; set; }
    public bool Include { get; set; }
}
