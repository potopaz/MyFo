using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Reports.DTOs;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Reports.Queries;

public class GetDrilldownQueryHandler : IRequestHandler<GetDrilldownQuery, DrilldownResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDrilldownQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DrilldownResultDto> Handle(GetDrilldownQuery request, CancellationToken ct)
    {
        var familyId = _currentUser.FamilyId!.Value;
        var family = await _db.Families.FirstOrDefaultAsync(f => f.FamilyId == familyId, ct);
        if (family is null) return new DrilldownResultDto();

        var useSecondary = !string.IsNullOrEmpty(family.SecondaryCurrencyCode)
                           && request.Currency == family.SecondaryCurrencyCode;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Base query (date-range applies; overridden for creditcard/installment cases below)
        var query = _db.Movements
            .Where(m => m.Date >= request.From && m.Date <= request.To);

        // Creditcard dimension: show movements contributing to current outstanding debt —
        // purchase date may predate the selected period, so we override the date-range filter.
        Guid filterCardId = Guid.Empty;
        bool isCreditCard = request.Dimension == "creditcard"
            && Guid.TryParse(request.DimensionValue, out filterCardId);

        // Credit card drilldown: show installment-level rows with converted amounts
        // so totals match the chart (which sums pending installments, not full movement amounts).
        if (isCreditCard)
        {
            // Build installment query — optionally filtered by month
            DateOnly? instMonthStart = null;
            DateOnly? instMonthEnd = null;
            if (!string.IsNullOrEmpty(request.InstallmentMonth)
                && DateOnly.TryParseExact(request.InstallmentMonth + "-01", "yyyy-MM-dd", out var instMonth))
            {
                instMonthStart = instMonth;
                instMonthEnd = instMonth.AddMonths(1).AddDays(-1);
            }

            var maxFutureDate = today.AddMonths(12);

            var installmentData = await (
                from i in _db.CreditCardInstallments
                where i.ActualAmount == null
                      && i.EstimatedDate >= today
                      && i.EstimatedDate <= maxFutureDate
                      && (instMonthStart == null || (i.EstimatedDate >= instMonthStart && i.EstimatedDate <= instMonthEnd))
                join p in _db.MovementPayments on i.MovementPaymentId equals p.MovementPaymentId
                where p.CreditCardId == filterCardId
                join m in _db.Movements on p.MovementId equals m.MovementId
                select new
                {
                    i.CreditCardInstallmentId,
                    i.EffectiveAmount,
                    i.InstallmentNumber,
                    i.EstimatedDate,
                    i.MovementPaymentId,
                    m.MovementId,
                    m.Date,
                    m.Description,
                    m.SubcategoryId,
                    m.CostCenterId,
                    m.IsOrdinary,
                    m.AccountingType,
                    m.RowVersion,
                    m.Amount,
                    m.AmountInPrimary,
                    m.AmountInSecondary,
                    m.MovementType,
                }
            ).ToListAsync(ct);

            // Total installments per payment for "Cuota X/Y" display
            var paymentIds = installmentData.Select(x => x.MovementPaymentId).Distinct().ToList();
            var totalByPayment = await _db.CreditCardInstallments
                .Where(i => paymentIds.Contains(i.MovementPaymentId))
                .GroupBy(i => i.MovementPaymentId)
                .Select(g => new { PaymentId = g.Key, Total = g.Count() })
                .ToListAsync(ct);
            var totalMap = totalByPayment.ToDictionary(x => x.PaymentId, x => x.Total);

            // Load subcategory/category/cost-center maps
            var subIds = installmentData.Select(x => x.SubcategoryId).Distinct().ToList();
            var subcatMap2 = await _db.Subcategories
                .Where(s => subIds.Contains(s.SubcategoryId))
                .Select(s => new { s.SubcategoryId, s.Name, s.CategoryId })
                .ToListAsync(ct);
            var catIds = subcatMap2.Select(s => s.CategoryId).Distinct().ToList();
            var catMap2 = await _db.Categories
                .Where(c => catIds.Contains(c.CategoryId))
                .Select(c => new { c.CategoryId, c.Name })
                .ToListAsync(ct);
            var ccIds = installmentData.Where(x => x.CostCenterId.HasValue).Select(x => x.CostCenterId!.Value).Distinct().ToList();
            var ccMap2 = await _db.CostCenters
                .Where(cc => ccIds.Contains(cc.CostCenterId))
                .Select(cc => new { cc.CostCenterId, cc.Name })
                .ToListAsync(ct);

            // Build installment-level rows with converted amounts
            var instRows = installmentData.Select(x =>
            {
                var sub = subcatMap2.FirstOrDefault(s => s.SubcategoryId == x.SubcategoryId);
                var cat = sub is not null ? catMap2.FirstOrDefault(c => c.CategoryId == sub.CategoryId) : null;
                var cc = x.CostCenterId.HasValue ? ccMap2.FirstOrDefault(c => c.CostCenterId == x.CostCenterId.Value) : null;
                var totalInst = totalMap.GetValueOrDefault(x.MovementPaymentId, 1);

                // Convert installment amount to report currency (same formula as chart)
                var convertedAmount = x.Amount > 0
                    ? x.EffectiveAmount * (useSecondary ? x.AmountInSecondary : x.AmountInPrimary) / x.Amount
                    : x.EffectiveAmount;

                var desc = x.Description ?? "";
                if (totalInst > 1)
                    desc += $" (Cuota {x.InstallmentNumber}/{totalInst})";

                return new DrilldownMovementDto
                {
                    MovementId = x.MovementId,
                    Date = x.EstimatedDate,
                    Description = desc,
                    SubcategoryId = x.SubcategoryId,
                    SubcategoryName = sub?.Name ?? "(Sin subcategoría)",
                    CategoryName = cat?.Name ?? "(Sin categoría)",
                    CostCenterId = x.CostCenterId,
                    CostCenterName = cc?.Name,
                    IsOrdinary = x.IsOrdinary,
                    AccountingType = x.AccountingType,
                    RowVersion = x.RowVersion,
                    Amount = convertedAmount,
                    CurrencyCode = request.Currency,
                    MovementType = x.MovementType.ToString(),
                };
            })
            .OrderBy(x => x.Date)
            .ThenByDescending(x => x.Amount)
            .ToList();

            var instTotalAmount = instRows.Sum(r => r.Amount);
            var instPage = Math.Max(1, request.Page);
            var instPageSize = Math.Clamp(request.PageSize, 1, 200);

            return new DrilldownResultDto
            {
                TotalCount = instRows.Count,
                TotalAmount = instTotalAmount,
                NetAmount = -instTotalAmount, // CC installments are always expense
                Items = instRows.Skip((instPage - 1) * instPageSize).Take(instPageSize).ToList(),
            };
        }

        // Movement type filter
        if (!string.IsNullOrEmpty(request.MovementType))
        {
            if (Enum.TryParse<MovementType>(request.MovementType, out var mt))
                query = query.Where(m => m.MovementType == mt);
        }

        var movements = await query
            .OrderByDescending(m => m.Date)
            .ToListAsync(ct);

        // Load subcategory/category maps
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

        var costCenterIds = movements.Where(m => m.CostCenterId.HasValue).Select(m => m.CostCenterId!.Value).Distinct().ToList();
        var ccMap = await _db.CostCenters
            .Where(cc => costCenterIds.Contains(cc.CostCenterId))
            .Select(cc => new { cc.CostCenterId, cc.Name })
            .ToListAsync(ct);

        // Build rows
        var rows = movements.Select(m =>
        {
            var sub = subcatMap.FirstOrDefault(s => s.SubcategoryId == m.SubcategoryId);
            var cat = sub is not null ? catMap.FirstOrDefault(c => c.CategoryId == sub.CategoryId) : null;
            var cc = m.CostCenterId.HasValue ? ccMap.FirstOrDefault(c => c.CostCenterId == m.CostCenterId.Value) : null;
            return new
            {
                Movement = m,
                SubcategoryName = sub?.Name ?? "(Sin subcategoría)",
                CategoryName = cat?.Name ?? "(Sin categoría)",
                CostCenterName = cc?.Name,
                Amount = useSecondary ? m.AmountInSecondary : m.AmountInPrimary,
            };
        }).ToList();

        // Apply dimension filter
        if (!string.IsNullOrEmpty(request.Dimension) && !string.IsNullOrEmpty(request.DimensionValue))
        {
            rows = request.Dimension switch
            {
                "subcategory" => rows.Where(r => r.SubcategoryName == request.DimensionValue).ToList(),
                "category"    => rows.Where(r => r.CategoryName == request.DimensionValue).ToList(),
                "costcenter"  => rows.Where(r => r.CostCenterName == request.DimensionValue).ToList(),
                "ordinary"    => request.DimensionValue == "true"
                    ? rows.Where(r => r.Movement.IsOrdinary == true).ToList()
                    : request.DimensionValue == "false"
                        ? rows.Where(r => r.Movement.IsOrdinary == false).ToList()
                        : rows.Where(r => r.Movement.IsOrdinary == null).ToList(),
                _ => rows
            };
        }

        var totalCount = rows.Count;
        var totalAmount = rows.Sum(r => r.Amount);
        var netAmount = rows.Sum(r => r.Movement.MovementType == MovementType.Income ? r.Amount : -r.Amount);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new DrilldownMovementDto
            {
                MovementId = r.Movement.MovementId,
                Date = r.Movement.Date,
                Description = r.Movement.Description,
                SubcategoryId = r.Movement.SubcategoryId,
                SubcategoryName = r.SubcategoryName,
                CategoryName = r.CategoryName,
                CostCenterId = r.Movement.CostCenterId,
                CostCenterName = r.CostCenterName,
                IsOrdinary = r.Movement.IsOrdinary,
                AccountingType = r.Movement.AccountingType,
                RowVersion = r.Movement.RowVersion,
                Amount = r.Amount,
                CurrencyCode = request.Currency,  // amount is already converted to report currency
                MovementType = r.Movement.MovementType.ToString(),
            })
            .ToList();

        return new DrilldownResultDto
        {
            TotalCount = totalCount,
            TotalAmount = totalAmount,
            NetAmount = netAmount,
            Items = items,
        };
    }
}
