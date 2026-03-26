using MediatR;
using MyFO.Application.CreditCards.CreditCardPayments.DTOs;

namespace MyFO.Application.CreditCards.CreditCardPayments.Commands;

public class CreateCreditCardPaymentCommand : IRequest<CreditCardPaymentDto>
{
    public Guid CreditCardId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public bool IsTotalPayment { get; set; }
    public Guid? StatementPeriodId { get; set; }
    public decimal PrimaryExchangeRate { get; set; } = 1;
    public decimal SecondaryExchangeRate { get; set; } = 1;
}
