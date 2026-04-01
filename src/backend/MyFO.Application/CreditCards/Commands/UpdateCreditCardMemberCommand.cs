using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.DTOs;

namespace MyFO.Application.CreditCards.Commands;

public class UpdateCreditCardMemberCommand : IRequest<CreditCardMemberDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CreditCardId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CreditCardMemberId { get; set; }
    public string HolderName { get; set; } = string.Empty;
    public string? LastFourDigits { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ExpirationMonth { get; set; }
    public int? ExpirationYear { get; set; }
    public Guid? MemberId { get; set; }
}
