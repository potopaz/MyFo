using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Reports.Queries;


public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardSummaryQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var familyId = _currentUser.FamilyId!.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Load family to determine currency mode
        var family = await _db.Families.FirstOrDefaultAsync(f => f.FamilyId == familyId, cancellationToken);
        if (family is null) return new DashboardSummaryDto();

        var useSecondary = !string.IsNullOrEmpty(family.SecondaryCurrencyCode)
                           && request.Currency == family.SecondaryCurrencyCode;

        // ── Patrimony ────────────────────────────────────────────────────────────

        // Load snapshots once for conversion
        var snapshots = await _db.ExchangeRateSnapshots
            .OrderByDescending(s => s.TargetDate)
            .ToListAsync(cancellationToken);

        var cashBoxes = await _db.CashBoxes.ToListAsync(cancellationToken);
        var bankAccounts = await _db.BankAccounts.ToListAsync(cancellationToken);

        decimal currentPatrimony = 0m;
        foreach (var cb in cashBoxes)
            currentPatrimony += ConvertBalance(cb.Balance, cb.CurrencyCode, request.Currency, snapshots);
        foreach (var ba in bankAccounts)
            currentPatrimony += ConvertBalance(ba.Balance, ba.CurrencyCode, request.Currency, snapshots);

        // ── Monthly flow (last 6 months) ─────────────────────────────────────────

        var sixMonthsAgo = today.AddMonths(-5);
        var startOf6Months = new DateOnly(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

        var movementsRaw = await _db.Movements
            .Where(m => m.Date >= startOf6Months)
            .ToListAsync(cancellationToken);

        // Build a 6-month skeleton
        var months = new List<(int Year, int Month)>();
        var cursor = startOf6Months;
        while (cursor <= today)
        {
            months.Add((cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }
        // Ensure exactly 6 (may be fewer if today is first day of month, but fine)

        var grouped = movementsRaw
            .GroupBy(m => (m.Date.Year, m.Date.Month, m.MovementType))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(m => useSecondary ? m.AmountInSecondary : m.AmountInPrimary));

        var monthlyFlow = months.Select(mo =>
        {
            var inc = grouped.GetValueOrDefault((mo.Year, mo.Month, MovementType.Income));
            var exp = grouped.GetValueOrDefault((mo.Year, mo.Month, MovementType.Expense));
            return new MonthlyFlowDto
            {
                Year = mo.Year,
                Month = mo.Month,
                Income = inc,
                Expense = exp,
                Result = inc - exp,
            };
        }).ToList();

        // ── Current and previous month figures ───────────────────────────────────

        var thisMonthIncome = monthlyFlow.FirstOrDefault(m => m.Year == today.Year && m.Month == today.Month)?.Income ?? 0m;
        var thisMonthExpense = monthlyFlow.FirstOrDefault(m => m.Year == today.Year && m.Month == today.Month)?.Expense ?? 0m;

        var prevMonth = today.AddMonths(-1);
        var lastMonthIncome = monthlyFlow.FirstOrDefault(m => m.Year == prevMonth.Year && m.Month == prevMonth.Month)?.Income ?? 0m;
        var lastMonthExpense = monthlyFlow.FirstOrDefault(m => m.Year == prevMonth.Year && m.Month == prevMonth.Month)?.Expense ?? 0m;

        // ── Patrimony evolution (roll-back) ───────────────────────────────────────

        var patrimonyEvolution = new List<MonthlyPatrimonyDto>(months.Count);
        // Fill in reverse: start from currentPatrimony for the last month slot
        var runningPatrimony = currentPatrimony;
        for (int i = monthlyFlow.Count - 1; i >= 0; i--)
        {
            var mf = monthlyFlow[i];
            patrimonyEvolution.Insert(0, new MonthlyPatrimonyDto
            {
                Year = mf.Year,
                Month = mf.Month,
                Balance = runningPatrimony,
            });
            // Roll back: the patrimony before this month = current - result of this month
            runningPatrimony -= (mf.Income - mf.Expense);
        }

        // ── PatrimonyChange = currentPatrimony - lastMonthPatrimony ──────────────
        var lastMonthPatrimonyEntry = patrimonyEvolution.FirstOrDefault(p => p.Year == prevMonth.Year && p.Month == prevMonth.Month);
        var patrimonyChange = lastMonthPatrimonyEntry is not null
            ? currentPatrimony - lastMonthPatrimonyEntry.Balance
            : 0m;

        // ── % changes ────────────────────────────────────────────────────────────
        decimal? incomeChangePct = lastMonthIncome > 0
            ? Math.Round((thisMonthIncome - lastMonthIncome) / lastMonthIncome * 100, 1)
            : null;
        decimal? expenseChangePct = lastMonthExpense > 0
            ? Math.Round((thisMonthExpense - lastMonthExpense) / lastMonthExpense * 100, 1)
            : null;

        return new DashboardSummaryDto
        {
            Patrimony = currentPatrimony,
            PatrimonyChange = patrimonyChange,
            MonthIncome = thisMonthIncome,
            MonthExpense = thisMonthExpense,
            MonthResult = thisMonthIncome - thisMonthExpense,
            MonthIncomeChangePct = incomeChangePct,
            MonthExpenseChangePct = expenseChangePct,
            MonthlyFlow = monthlyFlow,
            PatrimonyEvolution = patrimonyEvolution,
        };
    }

    /// <summary>
    /// Converts a balance from sourceCurrency to targetCurrency using the most recent snapshot.
    /// Returns 0 if no conversion rate is available (never blocks the dashboard).
    /// </summary>
    private static decimal ConvertBalance(
        decimal balance,
        string sourceCurrency,
        string targetCurrency,
        List<Domain.Common.ExchangeRateSnapshot> snapshots)
    {
        if (sourceCurrency == targetCurrency) return balance;
        if (balance == 0m) return 0m;

        // Try: snapshot with BaseCurrency = sourceCurrency, extract rate for targetCurrency
        var snap = snapshots.FirstOrDefault(s => s.BaseCurrency == sourceCurrency);
        if (snap is not null)
        {
            var rate = ExtractRate(snap.RatesJson, targetCurrency);
            if (rate.HasValue) return balance * rate.Value;
        }

        // Try inverse: snapshot with BaseCurrency = targetCurrency, extract rate for sourceCurrency
        var snapInverse = snapshots.FirstOrDefault(s => s.BaseCurrency == targetCurrency);
        if (snapInverse is not null)
        {
            var rateInverse = ExtractRate(snapInverse.RatesJson, sourceCurrency);
            if (rateInverse.HasValue && rateInverse.Value != 0m)
                return balance / rateInverse.Value;
        }

        // No rate available — contribute 0
        return 0m;
    }

    private static decimal? ExtractRate(string ratesJson, string currencyCode)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, decimal>>(ratesJson);
            return dict is not null && dict.TryGetValue(currencyCode, out var rate) ? rate : null;
        }
        catch
        {
            return null;
        }
    }
}
