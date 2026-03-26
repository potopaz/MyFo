using MyFO.Domain.Common;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Domain.Transactions;

public class Movement : TenantEntity
{
    public Guid MovementId { get; set; }
    public DateOnly Date { get; set; }
    public MovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal PrimaryExchangeRate { get; set; } = 1m;
    public decimal SecondaryExchangeRate { get; set; } = 1m;
    public decimal AmountInPrimary { get; set; }
    public decimal AmountInSecondary { get; set; }
    public string? Description { get; set; }
    public Guid SubcategoryId { get; set; }
    public string? AccountingType { get; set; }
    public bool? IsOrdinary { get; set; }
    public Guid? CostCenterId { get; set; }

    public string Source { get; set; } = "Web";
    public int RowVersion { get; set; } = 1;

    public ICollection<MovementPayment> Payments { get; set; } = [];
}
