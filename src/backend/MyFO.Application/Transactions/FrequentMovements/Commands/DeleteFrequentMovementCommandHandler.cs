using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.FrequentMovements.Commands;

public class DeleteFrequentMovementCommandHandler : IRequestHandler<DeleteFrequentMovementCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteFrequentMovementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteFrequentMovementCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var entity = await _db.FrequentMovements
            .FirstOrDefaultAsync(f => f.FamilyId == familyId && f.FrequentMovementId == request.FrequentMovementId, cancellationToken)
            ?? throw new NotFoundException("FrequentMovement", request.FrequentMovementId);

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
