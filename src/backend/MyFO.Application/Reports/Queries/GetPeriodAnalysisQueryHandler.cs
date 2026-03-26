using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Reports.Queries;

public class GetPeriodAnalysisQueryHandler : IRequestHandler<GetPeriodAnalysisQuery, PeriodAnalysisDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPeriodAnalysisQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PeriodAnalysisDto> Handle(GetPeriodAnalysisQuery request, CancellationToken cancellationToken)
    {
        var familyId = _currentUser.FamilyId!.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Determine currency mode
        var family = await _db.Families.FirstOrDefaultAsync(f => f.FamilyId == familyId, cancellationToken);
        if (family is null) return new PeriodAnalysisDto();

        var useSecondary = !string.IsNullOrEmpty(family.SecondaryCurrencyCode)
                           && request.Currency == family.SecondaryCurrencyCode;

        // ── Resolve period ────────────────────────────────────────────────────────
        var (dateFrom, dateTo) = ResolvePeriod(request.Period, today);

        // ── Load movements in period (only Income and Expense) ───────────────────
        var movements = await _db.Movements
            .Where(m => m.Date >= dateFrom && m.Date <= dateTo
                        && (m.MovementType == MovementType.Income || m.MovementType == MovementType.Expense))
            .ToListAsync(cancellationToken);

        if (movements.Count == 0)
            return new PeriodAnalysisDto();

        // ── Resolve related names in bulk ─────────────────────────────────────────
        var subcategoryIds = movements.Select(m => m.SubcategoryId).Distinct().ToList();

        var subcatMap = await _db.Subcategories
            .Where(s => subcategoryIds.Contains(s.SubcategoryId))
            .Select(s => new { s.SubcategoryId, s.Name, s.CategoryId })
            .ToListAsync(cancellationToken);

        var categoryIds = subcatMap.Select(s => s.CategoryId).Distinct().ToList();
        var catMap = await _db.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .Select(c => new { c.CategoryId, c.Name })
            .ToListAsync(cancellationToken);

        var costCenterIds = movements
            .Where(m => m.CostCenterId.HasValue)
            .Select(m => m.CostCenterId!.Value)
            .Distinct()
            .ToList();
        var costCenterMap = await _db.CostCenters
            .Where(cc => costCenterIds.Contains(cc.CostCenterId))
            .Select(cc => new { cc.CostCenterId, cc.Name })
            .ToListAsync(cancellationToken);

        // ── Build enriched rows ───────────────────────────────────────────────────
        var rows = movements.Select(m =>
        {
            var subcat = subcatMap.FirstOrDefault(s => s.SubcategoryId == m.SubcategoryId);
            var cat = subcat is not null ? catMap.FirstOrDefault(c => c.CategoryId == subcat.CategoryId) : null;
            var cc = m.CostCenterId.HasValue ? costCenterMap.FirstOrDefault(c => c.CostCenterId == m.CostCenterId.Value) : null;
            var amount = useSecondary ? m.AmountInSecondary : m.AmountInPrimary;
            return new
            {
                Amount = amount,
                m.MovementType,
                CategoryName = cat?.Name ?? "(Sin categoría)",
                SubcategoryName = subcat?.Name ?? "(Sin subcategoría)",
                CostCenterName = cc?.Name ?? "(Sin especificar)",
                CharacterName = m.IsOrdinary.HasValue
                    ? (m.IsOrdinary.Value ? "Ordinario" : "Extraordinario")
                    : "(Sin especificar)",
                AccountingTypeName = m.AccountingType ?? "(Sin especificar)",
            };
        }).ToList();

        // ── Totals ────────────────────────────────────────────────────────────────
        var totalIncome = rows.Where(r => r.MovementType == MovementType.Income).Sum(r => r.Amount);
        var totalExpense = rows.Where(r => r.MovementType == MovementType.Expense).Sum(r => r.Amount);

        // ── Dimension helper ──────────────────────────────────────────────────────
        static List<DimensionItemDto> BuildDimension<T>(
            IEnumerable<T> source,
            Func<T, string> nameSelector,
            Func<T, MovementType> typeSelector,
            Func<T, decimal> amountSelector)
        {
            return source
                .GroupBy(nameSelector)
                .Select(g => new DimensionItemDto
                {
                    Name = g.Key,
                    Income = g.Where(r => typeSelector(r) == MovementType.Income).Sum(amountSelector),
                    Expense = g.Where(r => typeSelector(r) == MovementType.Expense).Sum(amountSelector),
                })
                .OrderByDescending(d => d.Expense + d.Income)
                .ToList();
        }

        return new PeriodAnalysisDto
        {
            Income = totalIncome,
            Expense = totalExpense,
            Result = totalIncome - totalExpense,
            ByCategory = BuildDimension(rows, r => r.CategoryName, r => r.MovementType, r => r.Amount),
            BySubcategory = BuildDimension(rows, r => r.SubcategoryName, r => r.MovementType, r => r.Amount),
            ByCostCenter = BuildDimension(rows, r => r.CostCenterName, r => r.MovementType, r => r.Amount),
            ByCharacter = BuildDimension(rows, r => r.CharacterName, r => r.MovementType, r => r.Amount),
            ByAccountingType = BuildDimension(rows, r => r.AccountingTypeName, r => r.MovementType, r => r.Amount),
        };
    }

    private static (DateOnly From, DateOnly To) ResolvePeriod(string period, DateOnly today)
    {
        return period switch
        {
            "mes-anterior" => (
                new DateOnly(today.Year, today.Month, 1).AddMonths(-1),
                new DateOnly(today.Year, today.Month, 1).AddDays(-1)),
            "trimestre" => (
                new DateOnly(today.Year, today.Month, 1).AddMonths(-3),
                today),
            "anio" => (
                new DateOnly(today.Year, 1, 1),
                today),
            _ => ( // "mes-actual" and default
                new DateOnly(today.Year, today.Month, 1),
                today),
        };
    }
}
