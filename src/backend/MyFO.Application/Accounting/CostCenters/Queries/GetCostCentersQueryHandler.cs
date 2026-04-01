using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.CostCenters.DTOs;
using MyFO.Application.Common.Interfaces;

namespace MyFO.Application.Accounting.CostCenters.Queries;

public class GetCostCentersQueryHandler : IRequestHandler<GetCostCentersQuery, List<CostCenterDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCostCentersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CostCenterDto>> Handle(GetCostCentersQuery request, CancellationToken cancellationToken)
    {
        return await _db.CostCenters
            .OrderBy(c => c.Name)
            .Select(c => new CostCenterDto
            {
                CostCenterId = c.CostCenterId,
                Name = c.Name,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
