using MyFO.Domain.Common;

namespace MyFO.Domain.CreditCards;

/// <summary>
/// Proration record: distributes a payment proportionally across statement lines.
/// Each allocation points to exactly ONE target: either an installment OR a line item.
/// Stores amounts in card currency, primary currency, and secondary currency.
/// </summary>
public class StatementPaymentAllocation : TenantEntity
{
    public Guid AllocationId { get; set; }
    public Guid CreditCardPaymentId { get; set; }
    public Guid? CreditCardInstallmentId { get; set; }
    public Guid? StatementLineItemId { get; set; }
    public decimal AmountCardCurrency { get; set; }
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
    public decimal PrimaryExchangeRate { get; set; }
    public decimal SecondaryExchangeRate { get; set; }

    public CreditCardPayment CreditCardPayment { get; set; } = null!;
    public CreditCardInstallment? CreditCardInstallment { get; set; }
    public StatementLineItem? StatementLineItem { get; set; }
}
