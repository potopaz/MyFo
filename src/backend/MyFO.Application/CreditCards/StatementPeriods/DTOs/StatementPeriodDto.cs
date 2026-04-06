namespace MyFO.Application.CreditCards.StatementPeriods.DTOs;

public class StatementPeriodDto
{
    public Guid StatementPeriodId { get; set; }
    public Guid CreditCardId { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal PreviousBalance { get; set; }
    public decimal InstallmentsTotal { get; set; }
    public decimal ChargesTotal { get; set; }
    public decimal BonificationsTotal { get; set; }
    public decimal StatementTotal { get; set; }
    public decimal PaymentsTotal { get; set; }
    public decimal PendingBalance { get; set; }
    public DateTime? ClosedAt { get; set; }
}
