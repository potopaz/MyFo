using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.Movements.DTOs;

namespace MyFO.Application.Transactions.Movements.Commands;

public class CreateMovementCommand : IRequest<MovementDto>
{
    public DateOnly Date { get; set; }
    public string? MovementType { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal PrimaryExchangeRate { get; set; } = 1m;
    public decimal SecondaryExchangeRate { get; set; } = 1m;
    public string? Description { get; set; }
    public Guid SubcategoryId { get; set; }
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public Guid? CostCenterId { get; set; }
    public List<CreateMovementPaymentItem> Payments { get; set; } = [];
    public string Source { get; set; } = "Web";
}

public class CreateMovementPaymentItem
{
    public Guid? MovementPaymentId { get; set; }
    public string PaymentMethodType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? CreditCardMemberId { get; set; }
    public int? Installments { get; set; }

    // Credit card bonification
    public string? BonificationType { get; set; }
    public decimal? BonificationValue { get; set; }
}
