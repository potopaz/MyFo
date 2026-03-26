using MyFO.Domain.Common;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Domain.Transactions;

public class MovementPayment : TenantEntity
{
    public Guid MovementPaymentId { get; set; }
    public Guid MovementId { get; set; }
    public PaymentMethodType PaymentMethodType { get; set; }
    public decimal Amount { get; set; }
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? CreditCardMemberId { get; set; }
    public int? Installments { get; set; }

    // Credit card bonification fields
    public BonificationType? BonificationType { get; set; }
    public decimal? BonificationValue { get; set; }
    public decimal? BonificationAmount { get; set; }
    public decimal? NetAmount { get; set; }

    public Movement Movement { get; set; } = null!;
    public ICollection<CreditCardInstallment> CreditCardInstallments { get; set; } = [];
}
