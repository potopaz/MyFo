using MyFO.Domain.Common;

namespace MyFO.Domain.CreditCards;

/// <summary>
/// Individual installment of a credit card purchase.
/// Created when a movement with credit card payment is registered.
/// </summary>
public class CreditCardInstallment : TenantEntity
{
    public Guid CreditCardInstallmentId { get; set; }
    public Guid MovementPaymentId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal ProjectedAmount { get; set; }
    public decimal BonificationApplied { get; set; }
    public decimal EffectiveAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public decimal? ActualBonificationAmount { get; set; }
    public DateOnly EstimatedDate { get; set; }
    public Guid? StatementPeriodId { get; set; }

    public StatementPeriod? StatementPeriod { get; set; }
}
