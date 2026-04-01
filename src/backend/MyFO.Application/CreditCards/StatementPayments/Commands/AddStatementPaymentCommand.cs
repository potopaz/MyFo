using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;

namespace MyFO.Application.CreditCards.StatementPayments.Commands;

public class AddStatementPaymentCommand : IRequest<StatementPaymentDto>
{
    public Guid StatementPeriodId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public decimal PrimaryExchangeRate { get; set; } = 1m;
    public decimal SecondaryExchangeRate { get; set; } = 1m;
    public bool IsTotalPayment { get; set; }
}
