using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Reports.Queries;

public class GetCashFlowQueryHandler : IRequestHandler<GetCashFlowQuery, CashFlowReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCashFlowQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CashFlowReportDto> Handle(GetCashFlowQuery request, CancellationToken ct)
    {
        var familyId = _currentUser.FamilyId!.Value;
        var family = await _db.Families.FirstOrDefaultAsync(f => f.FamilyId == familyId, ct);
        if (family is null) return new CashFlowReportDto();

        var useSecondary = !string.IsNullOrEmpty(family.SecondaryCurrencyCode)
                           && request.Currency == family.SecondaryCurrencyCode;

        // ── Load movements in range ───────────────────────────────────────────────
        var movements = await _db.Movements
            .Where(m => m.Date >= request.From && m.Date <= request.To
                        && (m.MovementType == MovementType.Income || m.MovementType == MovementType.Expense))
            .Select(m => new
            {
                m.Date,
                m.MovementType,
                Amount = useSecondary ? m.AmountInSecondary : m.AmountInPrimary,
                m.MovementId,
            })
            .ToListAsync(ct);

        // ── Load payments for payment method breakdown ────────────────────────────
        var movementIds = movements.Select(m => m.MovementId).ToList();
        var expenseMovementIds = movements.Where(m => m.MovementType == MovementType.Expense)
            .Select(m => m.MovementId).ToHashSet();

        var payments = await _db.MovementPayments
            .Where(p => movementIds.Contains(p.MovementId))
            .Select(p => new { p.MovementId, p.PaymentMethodType, p.Amount })
            .ToListAsync(ct);

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

        // ── Cash flow ─────────────────────────────────────────────────────────────
        var cashFlow = movements
            .GroupBy(m => GetBucket(m.Date))
            .OrderBy(g => g.Key)
            .Select(g => new CashFlowPointDto
            {
                Label   = GetLabel(g.Key),
                Income  = g.Where(m => m.MovementType == MovementType.Income).Sum(m => m.Amount),
                Expense = g.Where(m => m.MovementType == MovementType.Expense).Sum(m => m.Amount),
                Net     = g.Where(m => m.MovementType == MovementType.Income).Sum(m => m.Amount)
                        - g.Where(m => m.MovementType == MovementType.Expense).Sum(m => m.Amount),
            })
            .ToList();

        // ── Payment methods ───────────────────────────────────────────────────────
        var expensePayments = payments.Where(p => expenseMovementIds.Contains(p.MovementId)).ToList();

        var paymentMethods = expensePayments
            .GroupBy(p => p.PaymentMethodType)
            .Select(g => new NameAmountDto
            {
                Name   = g.Key switch { PaymentMethodType.CashBox => "Efectivo", PaymentMethodType.BankAccount => "Débito", _ => "Tarjeta" },
                Amount = g.Sum(p => p.Amount),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // ── Payment method evolution ───────────────────────────────────────────────
        var pmLabels = new[] { "Efectivo", "Débito", "Tarjeta" };
        var pmEvolution = movements
            .Where(m => m.MovementType == MovementType.Expense)
            .GroupBy(m => GetBucket(m.Date))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var ids = g.Select(m => m.MovementId).ToHashSet();
                var pmGroup = expensePayments.Where(p => ids.Contains(p.MovementId)).ToList();
                return new TimeSeriesMultiDto
                {
                    Label = GetLabel(g.Key),
                    Values = pmLabels.ToDictionary(
                        lbl => lbl,
                        lbl => pmGroup.Where(p => p.PaymentMethodType switch
                        {
                            PaymentMethodType.CashBox => "Efectivo",
                            PaymentMethodType.BankAccount => "Débito",
                            _ => "Tarjeta",
                        } == lbl).Sum(p => p.Amount)),
                };
            })
            .ToList();

        // ── Future installments (next 12 months from today) ───────────────────────
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var futureFrom = today;
        var futureTo = today.AddMonths(12);

        var futureInstallments = await _db.CreditCardInstallments
            .Where(i => i.EstimatedDate >= futureFrom && i.EstimatedDate <= futureTo)
            .Join(_db.MovementPayments, i => i.MovementPaymentId, p => p.MovementPaymentId, (i, p) => new { i, p })
            .Join(_db.CreditCards, x => x.p.CreditCardId, cc => cc.CreditCardId, (x, cc) => new
            {
                x.i.EstimatedDate,
                Amount = useSecondary ? x.i.EffectiveAmount / 1m : x.i.EffectiveAmount,  // simplified — no ER conversion
                CardName = cc.Name,
            })
            .ToListAsync(ct);

        var futureByMonth = futureInstallments
            .GroupBy(x => new DateOnly(x.EstimatedDate.Year, x.EstimatedDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new FutureInstallmentDto
            {
                Label    = g.Key.ToString("MMM yy"),
                Amount   = g.Sum(x => x.Amount),
                CardName = string.Join(", ", g.Select(x => x.CardName).Distinct().Take(3)),
            })
            .ToList();

        return new CashFlowReportDto
        {
            Granularity           = granularity,
            CashFlow              = cashFlow,
            FutureInstallments    = futureByMonth,
            PaymentMethods        = paymentMethods,
            PaymentMethodEvolution = pmEvolution,
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
