using MyFO.Application.Common.Mediator;
using MyFO.Application.CreditCards.DTOs;

namespace MyFO.Application.CreditCards.Commands;

public class UpdateCreditCardCommand : IRequest<CreditCardDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CreditCardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string? HolderName { get; set; }
    public string? LastFourDigits { get; set; }
    public bool IsActive { get; set; } = true;
}
