namespace MyFO.Application.CreditCards.StatementPeriods.DTOs;

public class StatementPeriodDetailDto : StatementPeriodDto
{
    public List<StatementInstallmentDto> Installments { get; set; } = [];
    public List<StatementLineItemDto> LineItems { get; set; } = [];
}

public class StatementInstallmentDto
{
    public Guid CreditCardInstallmentId { get; set; }
    public Guid MovementPaymentId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal ProjectedAmount { get; set; }
    public decimal BonificationApplied { get; set; }
    public decimal EffectiveAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateOnly EstimatedDate { get; set; }
    public string? MovementDescription { get; set; }
    public DateOnly? MovementDate { get; set; }
    public int? TotalInstallments { get; set; }
    public bool IsIncluded { get; set; }
    public decimal? ActualBonificationAmount { get; set; }
    public bool IsBonificationIncluded { get; set; }
}

public class StatementLineItemDto
{
    public Guid StatementLineItemId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class StatementPaymentDto
{
    public Guid StatementPaymentId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public Guid? CashBoxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public decimal PrimaryExchangeRate { get; set; }
    public decimal SecondaryExchangeRate { get; set; }
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
    public bool IsTotalPayment { get; set; }
}
