using MyFO.Domain.Common;

namespace MyFO.Domain.CreditCards;

/// <summary>
/// A physical plastic card associated with a credit card account.
///
/// Each family member who uses the card gets their own CreditCardMember record.
/// This allows tracking purchases per person.
///
/// The "titular" is the main card holder. "Adicional" are extra cards.
///
/// Examples:
///   CreditCard "Visa Banco Nación"
///     → CreditCardMember "Juan" (titular)
///     → CreditCardMember "María" (adicional)
/// </summary>
public class CreditCardMember : TenantEntity
{
    public Guid CreditCardMemberId { get; set; }
    public Guid CreditCardId { get; set; }
    public string HolderName { get; set; } = string.Empty;
    public string? LastFourDigits { get; set; }
    public bool IsPrimary { get; set; }         // true = titular, false = adicional
    public bool IsActive { get; set; } = true;
    public Guid? MemberId { get; set; }         // linked FamilyMember (nullable)
    public int? ExpirationMonth { get; set; }   // 1-12
    public int? ExpirationYear { get; set; }    // e.g. 2027

    public CreditCard CreditCard { get; set; } = null!;
}
