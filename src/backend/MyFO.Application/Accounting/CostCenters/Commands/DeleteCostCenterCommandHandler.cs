using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.CostCenters.Commands;

public class DeleteCostCenterCommandHandler : IRequestHandler<DeleteCostCenterCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCostCenterCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCostCenterCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.CostCenters
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.CostCenterId == request.CostCenterId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("CostCenter", request.CostCenterId);

        var hasMovements = await _db.Movements
            .AnyAsync(m => m.CostCenterId == request.CostCenterId, cancellationToken);

        var hasFrequentMovements = await _db.FrequentMovements
            .AnyAsync(fm => fm.CostCenterId == request.CostCenterId, cancellationToken);

        if (hasMovements || hasFrequentMovements)
            throw new DomainException("COST_CENTER_IN_USE",
                "Este centro de costo ya fue utilizado en movimientos. No se puede eliminar.");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
