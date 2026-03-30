using MediatR;
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

        // Base query
        var query = _db.Movements
            .Where(m => m.Date >= request.From && m.Date <= request.To);

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
                    : rows.Where(r => r.Movement.IsOrdinary == false).ToList(),
                _ => rows
            };
        }

        var totalCount = rows.Count;
        var totalAmount = rows.Sum(r => r.Amount);

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
                SubcategoryName = r.SubcategoryName,
                CategoryName = r.CategoryName,
                Amount = r.Amount,
                CurrencyCode = r.Movement.CurrencyCode,
                MovementType = r.Movement.MovementType.ToString(),
            })
            .ToList();

        return new DrilldownResultDto
        {
            TotalCount = totalCount,
            TotalAmount = totalAmount,
            Items = items,
        };
    }
}
