using MyFO.Domain.Common;

namespace MyFO.Domain.CreditCards;

/// <summary>
/// A credit card account. Name should include brand + bank, e.g. "Visa Banco Nación".
/// Each card operates in a single currency. For dual-currency cards,
/// create two CreditCard records (one ARS, one USD).
/// </summary>
public class CreditCard : TenantEntity
{
    public Guid CreditCardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<CreditCardMember> Members { get; set; } = [];
    public ICollection<StatementPeriod> StatementPeriods { get; set; } = [];
}
