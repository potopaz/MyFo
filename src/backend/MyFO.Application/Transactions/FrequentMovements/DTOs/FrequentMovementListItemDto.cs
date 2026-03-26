namespace MyFO.Application.Transactions.FrequentMovements.DTOs;

public class FrequentMovementListItemDto
{
    public Guid FrequentMovementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string PaymentMethodType { get; set; } = string.Empty;
    public string? PaymentEntityName { get; set; }
    public int FrequencyMonths { get; set; }
    public DateTime? LastAppliedAt { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public bool IsActive { get; set; }
}
