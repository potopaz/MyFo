using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;

namespace MyFO.Application.Reports.Queries;

public class GetDisponibilidadesQueryHandler : IRequestHandler<GetDisponibilidadesQuery, DisponibilidadesDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IExchangeRateService _exchangeRateService;

    public GetDisponibilidadesQueryHandler(IApplicationDbContext db, IExchangeRateService exchangeRateService)
    {
        _db = db;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<DisponibilidadesDto> Handle(GetDisponibilidadesQuery request, CancellationToken cancellationToken)
    {
        var requestedCurrency = request.Currency.ToUpperInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Load all active accounts
        var cashBoxes = await _db.CashBoxes
            .Where(c => c.IsActive && c.Balance != 0)
            .Select(c => new { c.Name, c.Balance, c.CurrencyCode, AccountType = "CashBox" })
            .ToListAsync(cancellationToken);

        var bankAccounts = await _db.BankAccounts
            .Where(b => b.IsActive && b.Balance != 0)
            .Select(b => new { b.Name, b.Balance, b.CurrencyCode, AccountType = "BankAccount" })
            .ToListAsync(cancellationToken);

        var allAccounts = cashBoxes
            .Select(c => (c.AccountType, c.Name, c.Balance, c.CurrencyCode))
            .Concat(bankAccounts.Select(b => (b.AccountType, b.Name, b.Balance, b.CurrencyCode)))
            .ToList();

        if (allAccounts.Count == 0)
            return new DisponibilidadesDto { RequestedCurrency = requestedCurrency };

        // Fetch conversion rates for each unique foreign currency (one call per currency)
        var foreignCurrencies = allAccounts
            .Select(a => a.CurrencyCode)
            .Distinct()
            .Where(c => c != requestedCurrency)
            .ToList();

        var rates = new Dictionary<string, decimal?>();
        foreach (var currency in foreignCurrencies)
        {
            rates[currency] = await _exchangeRateService.GetRateAsync(
                currency, requestedCurrency, today, cancellationToken);
        }

        // Build groups
        var groups = allAccounts
            .GroupBy(a => a.CurrencyCode)
            .Select(g =>
            {
                var isRequestedCurrency = g.Key == requestedCurrency;
                var rate = isRequestedCurrency ? 1m : (rates.GetValueOrDefault(g.Key));

                var accounts = g.Select(a =>
                {
                    decimal? converted = rate.HasValue ? Math.Round(a.Balance * rate.Value, 2) : null;
                    return new AccountBalanceDto
                    {
                        AccountType = a.AccountType,
                        Name = a.Name,
                        Balance = a.Balance,
                        CurrencyCode = a.CurrencyCode,
                        BalanceConverted = converted,
                    };
                }).OrderByDescending(a => a.Balance).ToList();

                var totalNative = g.Sum(a => a.Balance);
                decimal? totalConverted = rate.HasValue
                    ? Math.Round(totalNative * rate.Value, 2)
                    : null;

                return new CurrencyGroupDto
                {
                    CurrencyCode = g.Key,
                    TotalNative = totalNative,
                    TotalConverted = totalConverted,
                    Accounts = accounts,
                };
            })
            .OrderByDescending(g => g.TotalConverted ?? 0m)
            .ToList();

        var totalConverted = groups.Sum(g => g.TotalConverted ?? 0m);

        return new DisponibilidadesDto
        {
            RequestedCurrency = requestedCurrency,
            TotalConverted = totalConverted,
            ByCurrency = groups,
        };
    }
}
