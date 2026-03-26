using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.Commands;

public class DeleteCreditCardCommandHandler : IRequestHandler<DeleteCreditCardCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCreditCardCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCreditCardCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.CreditCards
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.CreditCardId == request.CreditCardId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("CreditCard", request.CreditCardId);

        var hasMovements = await _db.MovementPayments
            .AnyAsync(mp => mp.CreditCardId == request.CreditCardId, cancellationToken);

        var hasStatements = await _db.StatementPeriods
            .AnyAsync(sp => sp.CreditCardId == request.CreditCardId, cancellationToken);

        var hasCCPayments = await _db.CreditCardPayments
            .AnyAsync(cp => cp.CreditCardId == request.CreditCardId, cancellationToken);

        var hasFrequentMovements = await _db.FrequentMovements
            .AnyAsync(fm => fm.CreditCardId == request.CreditCardId, cancellationToken);

        if (hasMovements || hasStatements || hasCCPayments || hasFrequentMovements)
            throw new DomainException("CREDIT_CARD_IN_USE",
                "Esta tarjeta ya fue utilizada en movimientos, resúmenes o pagos. No se puede eliminar.");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
