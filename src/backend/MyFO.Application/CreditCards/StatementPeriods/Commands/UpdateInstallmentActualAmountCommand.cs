using MediatR;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class UpdateInstallmentActualAmountCommand : IRequest
{
    public Guid CreditCardInstallmentId { get; set; }
    public decimal? ActualAmount { get; set; }
}
