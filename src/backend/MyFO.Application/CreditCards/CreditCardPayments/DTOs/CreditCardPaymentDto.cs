namespace MyFO.Application.CreditCards.CreditCardPayments.DTOs;

public class CreditCardPaymentDto
{
    public Guid CreditCardPaymentId { get; set; }
    public Guid CreditCardId { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? CashBoxId { get; set; }
    public string? CashBoxName { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? BankAccountName { get; set; }
    public bool IsTotalPayment { get; set; }
    public Guid? StatementPeriodId { get; set; }
    public bool IsPeriodClosed { get; set; }
    public decimal PrimaryExchangeRate { get; set; }
    public decimal SecondaryExchangeRate { get; set; }
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
}
