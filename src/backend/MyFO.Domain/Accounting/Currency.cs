using MyFO.Domain.Common;

namespace MyFO.Domain.Accounting;

/// <summary>
/// Global currency catalog. NOT tenant-scoped.
///
/// This table is managed by the system administrator and contains
/// all available currencies (ARS, USD, EUR, etc.).
///
/// Families don't create currencies — they select which ones to use
/// via the FamilyCurrency association table.
/// </summary>
public class Currency : BaseEntity
{
    public Guid CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;       // ISO 4217: "ARS", "USD"
    public string Name { get; set; } = string.Empty;       // "Peso Argentino", "Dólar"
    public string Symbol { get; set; } = string.Empty;     // "$", "US$", "€"
    public int DecimalPlaces { get; set; } = 2;
}
