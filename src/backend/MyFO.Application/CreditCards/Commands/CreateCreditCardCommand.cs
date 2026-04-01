using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.DTOs;

namespace MyFO.Application.CreditCards.Commands;

public class CreateCreditCardCommand : IRequest<CreditCardDto>
{
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public List<CreateCreditCardMemberItem>? Members { get; set; }
}

public class CreateCreditCardMemberItem
{
    public string HolderName { get; set; } = string.Empty;
    public string? LastFourDigits { get; set; }
    public bool IsPrimary { get; set; }
}
