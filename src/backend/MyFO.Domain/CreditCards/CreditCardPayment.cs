using MyFO.Domain.Common;

namespace MyFO.Domain.CreditCards;

public class CreditCardPayment : TenantEntity
{
    public Guid CreditCardPaymentId { get; set; }
    public Guid CreditCardId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    // Payment source: exactly one of these must be set
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }

    // Payment type
    public bool IsTotalPayment { get; set; }

    // Optional association to a statement period
    public Guid? StatementPeriodId { get; set; }

    // Exchange rates (for bimonetary support)
    public decimal PrimaryExchangeRate { get; set; } = 1;
    public decimal SecondaryExchangeRate { get; set; } = 1;
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }

    // Navigation
    public CreditCard CreditCard { get; set; } = null!;
    public StatementPeriod? StatementPeriod { get; set; }
    public ICollection<StatementPaymentAllocation> Allocations { get; set; } = [];
}
