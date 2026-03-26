using MediatR;
using MyFO.Application.Transactions.FrequentMovements.DTOs;

namespace MyFO.Application.Transactions.FrequentMovements.Commands;

public class UpdateFrequentMovementCommand : IRequest<FrequentMovementDto>
{
    public Guid FrequentMovementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? MovementType { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SubcategoryId { get; set; }
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public Guid? CostCenterId { get; set; }
    public string PaymentMethodType { get; set; } = string.Empty;
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? CreditCardMemberId { get; set; }
    public int FrequencyMonths { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public bool IsActive { get; set; }
    public int? ClientRowVersion { get; set; }
}
