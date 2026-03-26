namespace MyFO.Application.Transactions.Movements.DTOs;

public class MovementListItemDto
{
    public Guid MovementId { get; set; }
    public DateOnly Date { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal AmountInPrimary { get; set; }
    public string? Description { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public string? CostCenterName { get; set; }
    public bool HasAssignedInstallments { get; set; }
}
