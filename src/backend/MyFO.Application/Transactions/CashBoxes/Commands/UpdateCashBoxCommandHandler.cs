using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.CashBoxes.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public class UpdateCashBoxCommandHandler : IRequestHandler<UpdateCashBoxCommand, CashBoxDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCashBoxCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CashBoxDto> Handle(UpdateCashBoxCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.CashBoxes
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.CashBoxId == request.CashBoxId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("CashBox", request.CashBoxId);

        if (entity.CurrencyCode != request.CurrencyCode)
        {
            var hasMovements = await _db.MovementPayments
                .AnyAsync(mp => mp.CashBoxId == entity.CashBoxId, cancellationToken);
            var hasTransfers = await _db.Transfers
                .AnyAsync(t => t.FromCashBoxId == entity.CashBoxId || t.ToCashBoxId == entity.CashBoxId, cancellationToken);
            var hasCCPayments = await _db.CreditCardPayments
                .AnyAsync(cp => cp.CashBoxId == entity.CashBoxId, cancellationToken);
            var hasFrequentMovements = await _db.FrequentMovements
                .AnyAsync(fm => fm.CashBoxId == entity.CashBoxId, cancellationToken);
            if (hasMovements || hasTransfers || hasCCPayments || hasFrequentMovements)
                throw new DomainException("HAS_OPERATIONS", "No se puede modificar la moneda porque la caja tiene operaciones asociadas.");
        }

        entity.Name = request.Name;
        entity.CurrencyCode = request.CurrencyCode;
        // Adjust running balance by the delta in initial balance
        entity.Balance = entity.Balance - entity.InitialBalance + request.InitialBalance;
        entity.InitialBalance = request.InitialBalance;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return new CashBoxDto
        {
            CashBoxId = entity.CashBoxId,
            Name = entity.Name,
            CurrencyCode = entity.CurrencyCode,
            InitialBalance = entity.InitialBalance,
            Balance = entity.Balance,
            IsActive = entity.IsActive
        };
    }
}
