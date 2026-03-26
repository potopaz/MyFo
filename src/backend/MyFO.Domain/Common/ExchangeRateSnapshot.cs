namespace MyFO.Domain.Common;

/// <summary>
/// Cached exchange rate snapshot fetched from an external provider.
/// Not a TenantEntity — shared across all families to minimize API calls.
/// </summary>
public class ExchangeRateSnapshot
{
    /// <summary>The base currency code (ISO 4217), e.g. "USD".</summary>
    public string BaseCurrency { get; set; } = string.Empty;

    /// <summary>
    /// The date for which the rate was requested.
    /// For weekends/holidays, this is the requested date (not necessarily a business day).
    /// The rate stored is the most recent available from the provider.
    /// </summary>
    public DateOnly TargetDate { get; set; }

    /// <summary>
    /// JSON dictionary of currency code → rate, e.g. { "ARS": 1200.5, "EUR": 0.92 }.
    /// </summary>
    public string RatesJson { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the data was fetched from the provider.</summary>
    public DateTime FetchedAt { get; set; }
}
