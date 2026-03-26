using MediatR;

namespace MyFO.Application.CreditCards.Commands;

public record DeleteCreditCardMemberCommand(Guid CreditCardId, Guid CreditCardMemberId) : IRequest;
