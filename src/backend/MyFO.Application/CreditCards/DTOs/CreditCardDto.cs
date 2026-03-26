namespace MyFO.Application.CreditCards.DTOs;

public class CreditCardDto
{
    public Guid CreditCardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<CreditCardMemberDto> Members { get; set; } = [];
}

public class CreditCardMemberDto
{
    public Guid CreditCardMemberId { get; set; }
    public string HolderName { get; set; } = string.Empty;
    public string? LastFourDigits { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public int? ExpirationMonth { get; set; }
    public int? ExpirationYear { get; set; }
    public Guid? MemberId { get; set; }
    public bool IsCurrentUser { get; set; }
}
