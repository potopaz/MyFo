namespace MyFO.Application.Transactions.Movements.DTOs;

public class MovementPaymentDto
{
    public Guid MovementPaymentId { get; set; }
    public string PaymentMethodType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? CreditCardMemberId { get; set; }
    public int? Installments { get; set; }
    public string? BonificationType { get; set; }
    public decimal? BonificationValue { get; set; }
    public decimal? BonificationAmount { get; set; }
    public decimal? NetAmount { get; set; }
    public bool HasAssignedInstallments { get; set; }
}
