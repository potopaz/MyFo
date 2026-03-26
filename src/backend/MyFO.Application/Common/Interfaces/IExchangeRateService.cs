namespace MyFO.Application.Common.Interfaces;

public interface IExchangeRateService
{
    /// <summary>
    /// Returns the exchange rate from <paramref name="baseCurrency"/> to <paramref name="targetCurrency"/>
    /// for the given date. Uses a DB cache; if not found, fetches from the external provider
    /// and caches the result under the requested date.
    /// Returns null if the rate cannot be obtained.
    /// </summary>
    Task<decimal?> GetRateAsync(
        string baseCurrency,
        string targetCurrency,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
