using MyFO.Domain.Common;
using MyFO.Domain.Transactions;

namespace MyFO.Domain.CreditCards;

/// <summary>
/// Proration record: distributes a payment proportionally across statement lines.
/// Target is ONE of: installment, installment bonification, or line item.
/// When movement_payment_id is set, this is an installment bonification row (amount is negative).
/// Stores amounts in card currency, primary currency, and secondary currency.
/// </summary>
public class StatementPaymentAllocation : TenantEntity
{
    public Guid AllocationId { get; set; }
    public Guid CreditCardPaymentId { get; set; }
    public Guid? CreditCardInstallmentId { get; set; }
    public Guid? MovementPaymentId { get; set; }
    public Guid? StatementLineItemId { get; set; }
    public decimal AmountCardCurrency { get; set; }
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
    public decimal PrimaryExchangeRate { get; set; }
    public decimal SecondaryExchangeRate { get; set; }

    public CreditCardPayment CreditCardPayment { get; set; } = null!;
    public CreditCardInstallment? CreditCardInstallment { get; set; }
    public MovementPayment? MovementPayment { get; set; }
    public StatementLineItem? StatementLineItem { get; set; }
}
