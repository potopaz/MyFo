using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Common;

namespace MyFO.Infrastructure.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IApplicationDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        IApplicationDbContext db,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ExchangeRateService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _apiKey = configuration["ExchangeRates:ApiKey"];
        _logger = logger;
    }

    public async Task<decimal?> GetRateAsync(
        string baseCurrency,
        string targetCurrency,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        baseCurrency = baseCurrency.ToUpperInvariant();
        targetCurrency = targetCurrency.ToUpperInvariant();

        if (baseCurrency == targetCurrency)
            return 1m;

        // Check cache
        var snapshot = await _db.ExchangeRateSnapshots
            .FirstOrDefaultAsync(s => s.BaseCurrency == baseCurrency && s.TargetDate == date, cancellationToken);

        if (snapshot is not null)
        {
            return ExtractRate(snapshot.RatesJson, targetCurrency);
        }

        // Fetch from API
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("ExchangeRates:ApiKey is not configured.");
            return null;
        }

        try
        {
            var url = $"https://v6.exchangerate-api.com/v6/{_apiKey}/latest/{baseCurrency}";
            var response = await _httpClient.GetStringAsync(url, cancellationToken);

            var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (!root.TryGetProperty("result", out var resultProp) || resultProp.GetString() != "success")
            {
                _logger.LogWarning("Exchange rate API returned non-success for base {Base}: {Response}", baseCurrency, response);
                return null;
            }

            var ratesElement = root.GetProperty("conversion_rates");
            var ratesJson = ratesElement.GetRawText();

            // Save to cache with the requested date
            var newSnapshot = new ExchangeRateSnapshot
            {
                BaseCurrency = baseCurrency,
                TargetDate = date,
                RatesJson = ratesJson,
                FetchedAt = DateTime.UtcNow
            };

            _db.ExchangeRateSnapshots.Add(newSnapshot);
            await _db.SaveChangesAsync(cancellationToken);

            return ExtractRate(ratesJson, targetCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange rate for {Base}/{Target}", baseCurrency, targetCurrency);
            return null;
        }
    }

    private static decimal? ExtractRate(string ratesJson, string targetCurrency)
    {
        try
        {
            var doc = JsonDocument.Parse(ratesJson);
            if (doc.RootElement.TryGetProperty(targetCurrency, out var rateProp))
            {
                return rateProp.GetDecimal();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
