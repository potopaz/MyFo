using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Reports.Queries;

public class GetPatrimonyQueryHandler : IRequestHandler<GetPatrimonyQuery, PatrimonyReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExchangeRateService _exchangeRateService;

    public GetPatrimonyQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IExchangeRateService exchangeRateService)
    {
        _db = db;
        _currentUser = currentUser;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<PatrimonyReportDto> Handle(GetPatrimonyQuery request, CancellationToken ct)
    {
        var familyId = _currentUser.FamilyId!.Value;
        var family = await _db.Families.FirstOrDefaultAsync(f => f.FamilyId == familyId, ct);
        if (family is null) return new PatrimonyReportDto();

        var useSecondary = !string.IsNullOrEmpty(family.SecondaryCurrencyCode)
                           && request.Currency == family.SecondaryCurrencyCode;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var targetCurrency = request.Currency;

        // ── Cash balances ─────────────────────────────────────────────────────────
        var cashBoxes = await _db.CashBoxes
            .Where(c => c.IsActive)
            .Select(c => new { c.Name, c.Balance, c.CurrencyCode, Type = "Efectivo" })
            .ToListAsync(ct);

        var bankAccounts = await _db.BankAccounts
            .Where(b => b.IsActive)
            .Select(b => new { b.Name, b.Balance, b.CurrencyCode, Type = "Banco" })
            .ToListAsync(ct);

        var allAccounts = cashBoxes
            .Select(c => (c.Name, c.Balance, c.CurrencyCode, c.Type))
            .Concat(bankAccounts.Select(b => (b.Name, b.Balance, b.CurrencyCode, b.Type)))
            .ToList();

        // ── Get exchange rates for conversion ─────────────────────────────────────
        var foreignCurrencies = allAccounts
            .Select(a => a.CurrencyCode)
            .Distinct()
            .Where(c => c != targetCurrency)
            .ToList();

        var rates = new Dictionary<string, decimal?>();
        foreach (var cur in foreignCurrencies)
            rates[cur] = await _exchangeRateService.GetRateAsync(cur, targetCurrency, today, ct);

        decimal Convert(decimal amount, string currency) =>
            currency == targetCurrency ? amount :
            rates.TryGetValue(currency, out var rate) && rate.HasValue ? amount * rate.Value : 0m;

        // ── Assets ────────────────────────────────────────────────────────────────
        var totalAssets = allAccounts.Sum(a => Convert(a.Balance, a.CurrencyCode));

        var balanceByCurrency = allAccounts
            .GroupBy(a => a.CurrencyCode)
            .Select(g => new NameAmountDto
            {
                Name = g.Key,
                Amount = Convert(g.Sum(a => a.Balance), g.Key),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var balanceByAccountType = allAccounts
            .GroupBy(a => a.Type)
            .Select(g => new NameAmountDto
            {
                Name = g.Key,
                Amount = g.Sum(a => Convert(a.Balance, a.CurrencyCode)),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var topAccounts = allAccounts
            .Select(a => new AccountBalanceItemDto
            {
                Name = a.Name,
                AccountType = a.Type,
                CurrencyCode = a.CurrencyCode,
                Balance = a.Balance,
                BalanceInReportCurrency = Convert(a.Balance, a.CurrencyCode),
            })
            .OrderByDescending(a => a.BalanceInReportCurrency)
            .Take(10)
            .ToList();

        // ── Liabilities (future CC installments) ──────────────────────────────────
        var totalLiabilities = await _db.CreditCardInstallments
            .Where(i => i.EstimatedDate >= today && i.ActualAmount == null)
            .SumAsync(i => i.EffectiveAmount, ct);

        // ── Period income / expense ───────────────────────────────────────────────
        var periodMovements = await _db.Movements
            .Where(m => m.Date >= request.From && m.Date <= request.To
                        && (m.MovementType == MovementType.Income || m.MovementType == MovementType.Expense))
            .Select(m => new { m.MovementType, Amount = useSecondary ? m.AmountInSecondary : m.AmountInPrimary, m.Date })
            .ToListAsync(ct);

        var periodIncome = periodMovements.Where(m => m.MovementType == MovementType.Income).Sum(m => m.Amount);
        var periodExpense = periodMovements.Where(m => m.MovementType == MovementType.Expense).Sum(m => m.Amount);
        var periodSavings = periodIncome - periodExpense;

        // ── Patrimony evolution (monthly, last 12 months) ────────────────────────
        // Use running balance snapshot: for each month compute cumulative income - expense
        var evolutionFrom = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        var evolutionMovements = await _db.Movements
            .Where(m => m.Date >= evolutionFrom && m.Date <= today
                        && (m.MovementType == MovementType.Income || m.MovementType == MovementType.Expense))
            .Select(m => new { m.MovementType, Amount = useSecondary ? m.AmountInSecondary : m.AmountInPrimary, m.Date })
            .ToListAsync(ct);

        var patrimonyEvolution = evolutionMovements
            .GroupBy(m => new DateOnly(m.Date.Year, m.Date.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new TimePointDto
            {
                Label = g.Key.ToString("MMM yy"),
                Amount = g.Where(m => m.MovementType == MovementType.Income).Sum(m => m.Amount)
                       - g.Where(m => m.MovementType == MovementType.Expense).Sum(m => m.Amount),
            })
            .ToList();

        return new PatrimonyReportDto
        {
            TotalAssets        = totalAssets,
            TotalLiabilities   = totalLiabilities,
            NetPatrimony       = totalAssets - totalLiabilities,
            PeriodIncome       = periodIncome,
            PeriodExpense      = periodExpense,
            PeriodSavings      = periodSavings,
            SavingsRatio       = periodIncome > 0 ? (periodSavings / periodIncome) : null,
            PatrimonyEvolution = patrimonyEvolution,
            BalanceByCurrency  = balanceByCurrency,
            BalanceByAccountType = balanceByAccountType,
            TopAccounts        = topAccounts,
        };
    }
}
