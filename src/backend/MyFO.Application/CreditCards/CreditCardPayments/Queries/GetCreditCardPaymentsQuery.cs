using MediatR;
using MyFO.Application.CreditCards.CreditCardPayments.DTOs;

namespace MyFO.Application.CreditCards.CreditCardPayments.Queries;

public record GetCreditCardPaymentsQuery(Guid? CreditCardId) : IRequest<List<CreditCardPaymentDto>>;
