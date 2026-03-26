using MyFO.Domain.Common;
using MyFO.Domain.CreditCards.Enums;

namespace MyFO.Domain.CreditCards;

/// <summary>
/// A billing cycle / statement for a credit card.
/// Lifecycle: ClosedAt null = Open, ClosedAt set = Closed.
/// Payment tracking: PaymentStatus (Unpaid → PartiallyPaid → FullyPaid)
/// </summary>
public class StatementPeriod : TenantEntity
{
    public Guid StatementPeriodId { get; set; }
    public Guid CreditCardId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public decimal PreviousBalance { get; set; }
    public decimal InstallmentsTotal { get; set; }
    public decimal ChargesTotal { get; set; }
    public decimal BonificationsTotal { get; set; }
    public decimal StatementTotal { get; set; }
    public decimal PaymentsTotal { get; set; }
    public decimal PendingBalance { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }

    public CreditCard CreditCard { get; set; } = null!;
    public ICollection<CreditCardInstallment> Installments { get; set; } = [];
    public ICollection<StatementLineItem> LineItems { get; set; } = [];
    public ICollection<CreditCardPayment> Payments { get; set; } = [];
}
