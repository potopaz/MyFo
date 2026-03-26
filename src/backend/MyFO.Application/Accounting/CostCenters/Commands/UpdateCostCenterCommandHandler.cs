using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.CostCenters.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.CostCenters.Commands;

public class UpdateCostCenterCommandHandler : IRequestHandler<UpdateCostCenterCommand, CostCenterDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCostCenterCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CostCenterDto> Handle(UpdateCostCenterCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.CostCenters
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.CostCenterId == request.CostCenterId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("CostCenter", request.CostCenterId);

        entity.Name = request.Name;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return new CostCenterDto
        {
            CostCenterId = entity.CostCenterId,
            Name = entity.Name,
            IsActive = entity.IsActive
        };
    }
}
