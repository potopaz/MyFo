using MyFO.Application.Common;
using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.Movements.DTOs;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Movements.Queries;

public class GetMovementsQueryHandler : IRequestHandler<GetMovementsQuery, List<MovementListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMovementsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MovementListItemDto>> Handle(GetMovementsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Movements.AsQueryable();

        if (request.DateFrom.HasValue)
            query = query.Where(m => m.Date >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(m => m.Date <= request.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(request.MovementType)
            && Enum.TryParse<MovementType>(request.MovementType, true, out var mt))
            query = query.Where(m => m.MovementType == mt);

        if (request.SubcategoryId.HasValue)
            query = query.Where(m => m.SubcategoryId == request.SubcategoryId.Value);

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var term = request.Description.Trim().ToLower();
            query = query.Where(m => m.Description != null &&
                PgFunctions.Unaccent(m.Description.ToLower()).Contains(PgFunctions.Unaccent(term)));
        }

        return await query
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.CreatedAt)
            .Join(_db.Subcategories,
                m => new { m.FamilyId, m.SubcategoryId },
                s => new { s.FamilyId, s.SubcategoryId },
                (m, s) => new { Movement = m, Subcategory = s })
            .Join(_db.Categories,
                ms => new { ms.Subcategory.FamilyId, ms.Subcategory.CategoryId },
                c => new { c.FamilyId, c.CategoryId },
                (ms, c) => new { ms.Movement, ms.Subcategory, Category = c })
            .GroupJoin(_db.CostCenters,
                msc => new { msc.Movement.FamilyId, CostCenterId = msc.Movement.CostCenterId ?? Guid.Empty },
                cc => new { cc.FamilyId, cc.CostCenterId },
                (msc, costCenters) => new { msc, CostCenter = costCenters.FirstOrDefault() })
            .Select(x => new MovementListItemDto
            {
                MovementId = x.msc.Movement.MovementId,
                Date = x.msc.Movement.Date,
                MovementType = x.msc.Movement.MovementType.ToString(),
                Amount = x.msc.Movement.Amount,
                CurrencyCode = x.msc.Movement.CurrencyCode,
                AmountInPrimary = x.msc.Movement.AmountInPrimary,
                Description = x.msc.Movement.Description,
                SubcategoryName = x.msc.Subcategory.Name,
                CategoryName = x.msc.Category.Name,
                AccountingType = x.msc.Movement.AccountingType != null ? x.msc.Movement.AccountingType.ToString() : null,
                IsOrdinary = x.msc.Movement.IsOrdinary,
                CostCenterName = x.CostCenter != null ? x.CostCenter.Name : null,
                HasAssignedInstallments = _db.MovementPayments
                    .Where(mp => mp.FamilyId == x.msc.Movement.FamilyId
                        && mp.MovementId == x.msc.Movement.MovementId
                        && mp.PaymentMethodType == PaymentMethodType.CreditCard
                        && mp.DeletedAt == null)
                    .Join(_db.CreditCardInstallments.Where(i => i.StatementPeriodId != null && i.DeletedAt == null),
                        mp => new { mp.FamilyId, mp.MovementPaymentId },
                        i => new { i.FamilyId, i.MovementPaymentId },
                        (mp, i) => i)
                    .Any(),
            })
            .ToListAsync(cancellationToken);
    }
}
