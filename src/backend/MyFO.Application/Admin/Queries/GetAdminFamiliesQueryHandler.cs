using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Admin.DTOs;
using MyFO.Application.Common.Interfaces;

namespace MyFO.Application.Admin.Queries;

public class GetAdminFamiliesQueryHandler : IRequestHandler<GetAdminFamiliesQuery, List<AdminFamilyListItemDto>>
{
    private readonly IAdminDbContext _db;

    public GetAdminFamiliesQueryHandler(IAdminDbContext db) => _db = db;

    public async Task<List<AdminFamilyListItemDto>> Handle(GetAdminFamiliesQuery request, CancellationToken cancellationToken)
    {
        var families = await _db.Families
            .Where(f => f.DeletedAt == null)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        var familyIds = families.Select(f => f.FamilyId).ToList();

        var configs = await _db.FamilyAdminConfigs
            .Where(c => familyIds.Contains(c.FamilyId) && c.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var memberCounts = await _db.FamilyMembers
            .Where(m => familyIds.Contains(m.FamilyId) && m.DeletedAt == null && m.IsActive)
            .GroupBy(m => m.FamilyId)
            .Select(g => new { FamilyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var subcategoryCounts = await _db.GetSubcategoryCountsByFamilyAsync(cancellationToken);
        var costCenterCounts = await _db.GetCostCenterCountsByFamilyAsync(cancellationToken);
        var movementCounts = await _db.GetMovementCountsByFamilyAsync(cancellationToken);
        var transferCounts = await _db.GetTransferCountsByFamilyAsync(cancellationToken);

        var configDict = configs.ToDictionary(c => c.FamilyId);
        var countDict = memberCounts.ToDictionary(m => m.FamilyId, m => m.Count);

        return families.Select(f =>
        {
            var cfg = configDict.GetValueOrDefault(f.FamilyId);
            return new AdminFamilyListItemDto
            {
                FamilyId = f.FamilyId,
                Name = f.Name,
                MemberCount = countDict.GetValueOrDefault(f.FamilyId, 0),
                IsEnabled = cfg?.IsEnabled ?? true,
                MaxMembers = cfg?.MaxMembers,
                Notes = cfg?.Notes,
                DisabledAt = cfg?.DisabledAt,
                DisabledReason = cfg?.DisabledReason,
                SubcategoryCount = subcategoryCounts.GetValueOrDefault(f.FamilyId, 0),
                CostCenterCount = costCenterCounts.GetValueOrDefault(f.FamilyId, 0),
                MovementCount = movementCounts.GetValueOrDefault(f.FamilyId, 0),
                TransferCount = transferCounts.GetValueOrDefault(f.FamilyId, 0),
            };
        }).ToList();
    }
}
