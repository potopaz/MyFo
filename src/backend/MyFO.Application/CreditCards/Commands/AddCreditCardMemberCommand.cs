using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.DTOs;

namespace MyFO.Application.CreditCards.Commands;

public class AddCreditCardMemberCommand : IRequest<CreditCardMemberDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CreditCardId { get; set; }
    public string HolderName { get; set; } = string.Empty;
    public string? LastFourDigits { get; set; }
    public bool IsPrimary { get; set; }
    public int? ExpirationMonth { get; set; }
    public int? ExpirationYear { get; set; }
    public Guid? MemberId { get; set; }
}
