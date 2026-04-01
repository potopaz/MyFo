using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Reports.Queries;

public class GetIncomeExpenseQueryHandler : IRequestHandler<GetIncomeExpenseQuery, IncomeExpenseReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetIncomeExpenseQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IncomeExpenseReportDto> Handle(GetIncomeExpenseQuery request, CancellationToken ct)
    {
        var familyId = _currentUser.FamilyId!.Value;
        var family = await _db.Families.FirstOrDefaultAsync(f => f.FamilyId == familyId, ct);
        if (family is null) return new IncomeExpenseReportDto();

        var useSecondary = !string.IsNullOrEmpty(family.SecondaryCurrencyCode)
                           && request.Currency == family.SecondaryCurrencyCode;

        // ── Load movements ────────────────────────────────────────────────────────
        var query = _db.Movements
            .Where(m => m.Date >= request.From && m.Date <= request.To
                        && (m.MovementType == MovementType.Income || m.MovementType == MovementType.Expense));

        if (request.CostCenterId.HasValue)
            query = query.Where(m => m.CostCenterId == request.CostCenterId);
        if (request.IsOrdinary.HasValue)
            query = query.Where(m => m.IsOrdinary == request.IsOrdinary);

        var movements = await query.ToListAsync(ct);

        // filter by subcategory / category (done in memory after joining names)
        var subcategoryIds = movements.Select(m => m.SubcategoryId).Distinct().ToList();
        var subcatMap = await _db.Subcategories
            .Where(s => subcategoryIds.Contains(s.SubcategoryId))
            .Select(s => new { s.SubcategoryId, s.Name, s.CategoryId })
            .ToListAsync(ct);

        var categoryIds = subcatMap.Select(s => s.CategoryId).Distinct().ToList();
        var catMap = await _db.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .Select(c => new { c.CategoryId, c.Name })
            .ToListAsync(ct);

        // Apply category / subcategory filter (optional)
        if (request.SubcategoryId.HasValue)
            movements = movements.Where(m => m.SubcategoryId == request.SubcategoryId.Value).ToList();
        else if (request.CategoryId.HasValue)
        {
            var subcatIdsInCat = subcatMap
                .Where(s => s.CategoryId == request.CategoryId.Value)
                .Select(s => s.SubcategoryId)
                .ToHashSet();
            movements = movements.Where(m => subcatIdsInCat.Contains(m.SubcategoryId)).ToList();
        }

        if (movements.Count == 0) return new IncomeExpenseReportDto();

        // ── Enrich rows ───────────────────────────────────────────────────────────
        var rows = movements.Select(m =>
        {
            var sub = subcatMap.FirstOrDefault(s => s.SubcategoryId == m.SubcategoryId);
            var cat = sub is not null ? catMap.FirstOrDefault(c => c.CategoryId == sub.CategoryId) : null;
            return new
            {
                m.Date,
                m.MovementType,
                Amount = useSecondary ? m.AmountInSecondary : m.AmountInPrimary,
                SubcategoryId = m.SubcategoryId.ToString(),
                SubcategoryName = sub?.Name ?? "(Sin subcategoría)",
                CategoryId = cat?.CategoryId.ToString() ?? "",
                CategoryName = cat?.Name ?? "(Sin categoría)",
                m.IsOrdinary,
            };
        }).ToList();

        var expenseRows = rows.Where(r => r.MovementType == MovementType.Expense).ToList();
        var incomeRows  = rows.Where(r => r.MovementType == MovementType.Income).ToList();

        var totalExpense = expenseRows.Sum(r => r.Amount);
        var totalIncome  = incomeRows.Sum(r => r.Amount);

        // ── Granularity ───────────────────────────────────────────────────────────
        var days = (request.To.ToDateTime(TimeOnly.MinValue) - request.From.ToDateTime(TimeOnly.MinValue)).Days + 1;
        var granularity = days <= 45 ? "daily" : days <= 180 ? "weekly" : "monthly";

        // ── Expense by subcategory (top 15) ───────────────────────────────────────
        var expenseBySubcat = expenseRows
            .GroupBy(r => r.SubcategoryName)
            .Select(g => new NameAmountDto { Name = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(15)
            .ToList();

        // ── Expense by category (hierarchical for treemap) ───────────────────────
        var expenseByCategory = expenseRows
            .GroupBy(r => r.CategoryName)
            .Select(g => new CategoryExpenseDto
            {
                CategoryName = g.Key,
                Amount = g.Sum(x => x.Amount),
                Subcategories = g.GroupBy(x => x.SubcategoryName)
                    .Select(sg => new NameAmountDto { Name = sg.Key, Amount = sg.Sum(x => x.Amount) })
                    .OrderByDescending(x => x.Amount)
                    .ToList(),
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // ── Ordinary vs Extraordinary ─────────────────────────────────────────────
        var ordVsExtra = new OrdVsExtraDto
        {
            Ordinary       = expenseRows.Where(r => r.IsOrdinary == true).Sum(r => r.Amount),
            Extraordinary  = expenseRows.Where(r => r.IsOrdinary == false).Sum(r => r.Amount),
            Unspecified    = expenseRows.Where(r => r.IsOrdinary == null).Sum(r => r.Amount),
        };

        // ── Expense by cost center ────────────────────────────────────────────────
        var expenseMovements = movements.Where(m => m.MovementType == MovementType.Expense).ToList();
        var ccIds = expenseMovements
            .Where(m => m.CostCenterId.HasValue)
            .Select(m => m.CostCenterId!.Value)
            .Distinct()
            .ToList();

        List<NameAmountDto> expenseByCostCenter = [];
        if (ccIds.Count > 0)
        {
            var ccNameMap = await _db.CostCenters
                .Where(cc => ccIds.Contains(cc.CostCenterId))
                .Select(cc => new { cc.CostCenterId, cc.Name })
                .ToListAsync(ct);

            expenseByCostCenter = expenseMovements
                .Where(m => m.CostCenterId.HasValue)
                .GroupBy(m => m.CostCenterId!.Value)
                .Select(g => new NameAmountDto
                {
                    Name = ccNameMap.FirstOrDefault(c => c.CostCenterId == g.Key)?.Name ?? "(Sin CC)",
                    Amount = g.Sum(m => useSecondary ? m.AmountInSecondary : m.AmountInPrimary),
                })
                .OrderByDescending(x => x.Amount)
                .ToList();
        }

        // ── Income by source (top subcategory) ───────────────────────────────────
        var incomeBySource = incomeRows
            .GroupBy(r => r.SubcategoryName)
            .Select(g => new NameAmountDto { Name = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // ── Time series helpers ───────────────────────────────────────────────────
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

        // ── Category evolution (top 5 expense categories) ────────────────────────
        var topCategories = expenseByCategory.Take(5).Select(c => c.CategoryName).ToList();

        var categoryEvolution = rows
            .Where(r => r.MovementType == MovementType.Expense && topCategories.Contains(r.CategoryName))
            .GroupBy(r => GetBucket(r.Date))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesMultiDto
            {
                Label = GetLabel(g.Key),
                Values = topCategories.ToDictionary(
                    cat => cat,
                    cat => g.Where(r => r.CategoryName == cat).Sum(r => r.Amount)),
            })
            .ToList();

        // ── Income evolution ──────────────────────────────────────────────────────
        var incomeEvolution = incomeRows
            .GroupBy(r => GetBucket(r.Date))
            .OrderBy(g => g.Key)
            .Select(g => new TimePointDto { Label = GetLabel(g.Key), Amount = g.Sum(r => r.Amount) })
            .ToList();

        return new IncomeExpenseReportDto
        {
            Granularity          = granularity,
            TotalExpense         = totalExpense,
            TotalIncome          = totalIncome,
            ExpenseBySubcategory = expenseBySubcat,
            ExpenseByCategory    = expenseByCategory,
            OrdVsExtra           = ordVsExtra,
            CategoryEvolution    = categoryEvolution,
            IncomeBySource       = incomeBySource,
            IncomeEvolution      = incomeEvolution,
            ExpenseByCostCenter  = expenseByCostCenter,
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
