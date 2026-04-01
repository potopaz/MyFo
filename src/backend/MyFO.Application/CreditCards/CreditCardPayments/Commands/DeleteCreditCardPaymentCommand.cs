using MyFO.Application.Common.Mediator;

namespace MyFO.Application.CreditCards.CreditCardPayments.Commands;

public record DeleteCreditCardPaymentCommand(Guid CreditCardPaymentId) : IRequest;
