using MyFO.Application.Common.Mediator;

namespace MyFO.Application.CreditCards.Commands;

public record DeleteCreditCardCommand(Guid CreditCardId) : IRequest;
