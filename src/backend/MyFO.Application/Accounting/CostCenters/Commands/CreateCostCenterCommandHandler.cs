using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.CostCenters.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.CostCenters.Commands;

public class CreateCostCenterCommandHandler : IRequestHandler<CreateCostCenterCommand, CostCenterDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCostCenterCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CostCenterDto> Handle(CreateCostCenterCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Check for existing (including soft-deleted) with same name
        var existing = await _db.CostCenters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.Name == request.Name, cancellationToken);

        CostCenter costCenter;

        if (existing is not null)
        {
            if (existing.DeletedAt is null)
                throw new DomainException("DUPLICATE_NAME", $"Ya existe un centro de costo con el nombre '{request.Name}'.");

            // Reactivate soft-deleted record
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.IsActive = true;
            costCenter = existing;
        }
        else
        {
            costCenter = new CostCenter
            {
                FamilyId = familyId,
                CostCenterId = Guid.NewGuid(),
                Name = request.Name
            };
            await _db.CostCenters.AddAsync(costCenter, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CostCenterDto
        {
            CostCenterId = costCenter.CostCenterId,
            Name = costCenter.Name,
            IsActive = costCenter.IsActive
        };
    }
}
