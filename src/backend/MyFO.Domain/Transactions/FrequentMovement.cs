using MyFO.Domain.Common;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Domain.Transactions;

public class FrequentMovement : TenantEntity
{
    public Guid FrequentMovementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SubcategoryId { get; set; }
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public Guid? CostCenterId { get; set; }

    // Single payment method
    public PaymentMethodType PaymentMethodType { get; set; }
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? CreditCardMemberId { get; set; }

    // Recurrence: null = no recurrence, N = every N months
    public int FrequencyMonths { get; set; }

    // Tracking
    public DateTime? LastAppliedAt { get; set; }
    public DateOnly? NextDueDate { get; set; }

    public bool IsActive { get; set; } = true;
    public int RowVersion { get; set; } = 1;
}
