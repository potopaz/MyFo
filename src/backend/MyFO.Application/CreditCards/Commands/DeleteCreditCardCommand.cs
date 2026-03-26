using MediatR;

namespace MyFO.Application.CreditCards.Commands;

public record DeleteCreditCardCommand(Guid CreditCardId) : IRequest;
