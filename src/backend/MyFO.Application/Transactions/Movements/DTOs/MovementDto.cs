namespace MyFO.Application.Transactions.Movements.DTOs;

public class MovementDto
{
    public Guid MovementId { get; set; }
    public DateOnly Date { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal PrimaryExchangeRate { get; set; }
    public decimal SecondaryExchangeRate { get; set; }
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
    public string? Description { get; set; }
    public Guid SubcategoryId { get; set; }
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public Guid? CostCenterId { get; set; }
    public string Source { get; set; } = "Web";
    public int RowVersion { get; set; }
    public List<MovementPaymentDto> Payments { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedByName { get; set; }
}
