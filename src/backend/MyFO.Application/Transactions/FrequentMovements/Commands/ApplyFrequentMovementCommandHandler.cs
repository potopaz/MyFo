using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.FrequentMovements.Commands;

public class ApplyFrequentMovementCommandHandler : IRequestHandler<ApplyFrequentMovementCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ApplyFrequentMovementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ApplyFrequentMovementCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var entity = await _db.FrequentMovements
            .FirstOrDefaultAsync(f => f.FamilyId == familyId && f.FrequentMovementId == request.FrequentMovementId, cancellationToken)
            ?? throw new NotFoundException("FrequentMovement", request.FrequentMovementId);

        entity.LastAppliedAt = DateTime.UtcNow;
        entity.NextDueDate = request.MovementDate.AddMonths(entity.FrequencyMonths);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
