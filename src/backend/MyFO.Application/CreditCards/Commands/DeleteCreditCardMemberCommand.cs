using MyFO.Application.Common.Mediator;

namespace MyFO.Application.CreditCards.Commands;

public record DeleteCreditCardMemberCommand(Guid CreditCardId, Guid CreditCardMemberId) : IRequest;
