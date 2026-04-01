using MyFO.Application.Common.Mediator;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class UpdateInstallmentBonificationAmountCommand : IRequest
{
    public Guid CreditCardInstallmentId { get; set; }
    public decimal? ActualBonificationAmount { get; set; }
}
