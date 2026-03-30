using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Reports.Queries;

public class GetCardsCCQueryHandler : IRequestHandler<GetCardsCCQuery, CardsCCReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCardsCCQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CardsCCReportDto> Handle(GetCardsCCQuery request, CancellationToken ct)
    {
        var familyId = _currentUser.FamilyId!.Value;
        var family = await _db.Families.FirstOrDefaultAsync(f => f.FamilyId == familyId, ct);
        if (family is null) return new CardsCCReportDto();

        var useSecondary = !string.IsNullOrEmpty(family.SecondaryCurrencyCode)
                           && request.Currency == family.SecondaryCurrencyCode;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ── Future installments (all pending) ─────────────────────────────────────
        var futureInstallments = await _db.CreditCardInstallments
            .Where(i => i.EstimatedDate >= today && i.ActualAmount == null)
            .Join(_db.MovementPayments, i => i.MovementPaymentId, p => p.MovementPaymentId,
                (i, p) => new { i.EffectiveAmount, p.CreditCardId })
            .Where(x => x.CreditCardId != null)
            .Join(_db.CreditCards, x => x.CreditCardId, cc => cc.CreditCardId,
                (x, cc) => new { x.EffectiveAmount, cc.Name })
            .ToListAsync(ct);

        var totalDebt = futureInstallments.Sum(x => x.EffectiveAmount);
        var installmentsByCard = futureInstallments
            .GroupBy(x => x.Name)
            .Select(g => new CardInstallmentsSummaryDto
            {
                CardName = g.Key,
                TotalDebt = g.Sum(x => x.EffectiveAmount),
                PendingInstallments = g.Count(),
            })
            .OrderByDescending(x => x.TotalDebt)
            .ToList();

        // ── CC payments in period ─────────────────────────────────────────────────
        var ccPayments = await _db.CreditCardPayments
            .Where(p => p.PaymentDate >= request.From && p.PaymentDate <= request.To)
            .ToListAsync(ct);

        var totalPaid = ccPayments.Sum(p => useSecondary ? p.AmountInSecondary : p.AmountInPrimary);

        // ── Cost center breakdown ─────────────────────────────────────────────────
        var movements = await _db.Movements
            .Where(m => m.Date >= request.From && m.Date <= request.To
                        && m.MovementType == MovementType.Expense
                        && m.CostCenterId != null)
            .Select(m => new { m.CostCenterId, Amount = useSecondary ? m.AmountInSecondary : m.AmountInPrimary, m.Date })
            .ToListAsync(ct);

        var costCenterIds = movements.Select(m => m.CostCenterId!.Value).Distinct().ToList();
        var ccNameMap = await _db.CostCenters
            .Where(cc => costCenterIds.Contains(cc.CostCenterId))
            .Select(cc => new { cc.CostCenterId, cc.Name })
            .ToListAsync(ct);

        var byCostCenter = movements
            .GroupBy(m => m.CostCenterId!.Value)
            .Select(g => new NameAmountDto
            {
                Name = ccNameMap.FirstOrDefault(c => c.CostCenterId == g.Key)?.Name ?? "(Sin CC)",
                Amount = g.Sum(m => m.Amount),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // ── Granularity ───────────────────────────────────────────────────────────
        var days = (request.To.ToDateTime(TimeOnly.MinValue) - request.From.ToDateTime(TimeOnly.MinValue)).Days + 1;
        var granularity = days <= 45 ? "daily" : days <= 180 ? "weekly" : "monthly";

        string GetLabel(DateOnly date) => granularity switch
        {
            "daily"  => date.ToString("dd/MM"),
            "weekly" => $"Sem {ISOWeek(date)}",
            _        => date.ToString("MMM yy"),
        };

        DateOnly GetBucket(DateOnly date) => granularity switch
        {
            "daily"  => date,
            "weekly" => StartOfISOWeek(date),
            _        => new DateOnly(date.Year, date.Month, 1),
        };

        // ── Cost center evolution ─────────────────────────────────────────────────
        var topCCs = byCostCenter.Take(5).Select(c => c.Name).ToList();
        var ccEvolution = movements
            .Where(m => topCCs.Contains(ccNameMap.FirstOrDefault(c => c.CostCenterId == m.CostCenterId!.Value)?.Name ?? ""))
            .GroupBy(m => GetBucket(m.Date))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesMultiDto
            {
                Label = GetLabel(g.Key),
                Values = topCCs.ToDictionary(
                    ccName => ccName,
                    ccName =>
                    {
                        var ccId = ccNameMap.FirstOrDefault(c => c.Name == ccName)?.CostCenterId;
                        return ccId.HasValue ? g.Where(m => m.CostCenterId == ccId.Value).Sum(m => m.Amount) : 0m;
                    }),
            })
            .ToList();

        // ── Charges vs bonifications (statement line items in period) ─────────────
        var statementsInPeriod = await _db.StatementPeriods
            .Where(s => s.DueDate >= request.From && s.DueDate <= request.To)
            .Select(s => s.StatementPeriodId)
            .ToListAsync(ct);

        var lineItems = await _db.StatementLineItems
            .Where(l => statementsInPeriod.Contains(l.StatementPeriodId))
            .Select(l => new { l.LineType, l.Amount })
            .ToListAsync(ct);

        var chargesVsBonifications = new ChargesVsBonificationsDto
        {
            TotalCharges       = lineItems.Where(l => l.LineType == StatementLineType.Charge).Sum(l => l.Amount),
            TotalBonifications = lineItems.Where(l => l.LineType == StatementLineType.Bonification).Sum(l => l.Amount),
            Net                = lineItems.Where(l => l.LineType == StatementLineType.Charge).Sum(l => l.Amount)
                               - lineItems.Where(l => l.LineType == StatementLineType.Bonification).Sum(l => l.Amount),
        };

        return new CardsCCReportDto
        {
            TotalDebt              = totalDebt,
            TotalPaid              = totalPaid,
            InstallmentsByCard     = installmentsByCard,
            ByCostCenter           = byCostCenter,
            CostCenterEvolution    = ccEvolution,
            ChargesVsBonifications = chargesVsBonifications,
            Granularity            = granularity,
        };
    }

    private static int ISOWeek(DateOnly date)
    {
        var d = date.ToDateTime(TimeOnly.MinValue);
        var day = (int)d.DayOfWeek;
        if (day == 0) day = 7;
        var thursday = d.AddDays(4 - day);
        var yearStart = new DateTime(thursday.Year, 1, 1);
        return (int)Math.Ceiling((thursday - yearStart).TotalDays / 7) + 1;
    }

    private static DateOnly StartOfISOWeek(DateOnly date)
    {
        var d = date.ToDateTime(TimeOnly.MinValue);
        var day = (int)d.DayOfWeek;
        if (day == 0) day = 7;
        return DateOnly.FromDateTime(d.AddDays(1 - day));
    }
}
