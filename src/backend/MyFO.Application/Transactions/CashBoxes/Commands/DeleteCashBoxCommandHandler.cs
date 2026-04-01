using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public class DeleteCashBoxCommandHandler : IRequestHandler<DeleteCashBoxCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCashBoxCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCashBoxCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.CashBoxes
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.CashBoxId == request.CashBoxId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("CashBox", request.CashBoxId);

        var hasMovements = await _db.MovementPayments
            .AnyAsync(mp => mp.CashBoxId == request.CashBoxId, cancellationToken);

        var hasTransfers = await _db.Transfers
            .AnyAsync(t => t.FromCashBoxId == request.CashBoxId || t.ToCashBoxId == request.CashBoxId, cancellationToken);

        var hasCCPayments = await _db.CreditCardPayments
            .AnyAsync(cp => cp.CashBoxId == request.CashBoxId, cancellationToken);

        var hasFrequentMovements = await _db.FrequentMovements
            .AnyAsync(fm => fm.CashBoxId == request.CashBoxId, cancellationToken);

        if (hasMovements || hasTransfers || hasCCPayments || hasFrequentMovements)
            throw new DomainException("CASHBOX_IN_USE",
                "Esta caja ya fue utilizada en movimientos, transferencias o pagos. No se puede eliminar.");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
