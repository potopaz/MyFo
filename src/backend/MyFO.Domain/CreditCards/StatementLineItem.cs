using MyFO.Domain.Common;
using MyFO.Domain.CreditCards.Enums;

namespace MyFO.Domain.CreditCards;

/// <summary>
/// A charge or bonification line in a credit card statement.
/// Charges = bank fees (interest, insurance, etc.) → interpreted as expense.
/// Bonifications = bank discounts (cashback, etc.) → interpreted as income.
/// </summary>
public class StatementLineItem : TenantEntity
{
    public Guid StatementLineItemId { get; set; }
    public Guid StatementPeriodId { get; set; }
    public StatementLineType LineType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public StatementPeriod StatementPeriod { get; set; } = null!;
}
