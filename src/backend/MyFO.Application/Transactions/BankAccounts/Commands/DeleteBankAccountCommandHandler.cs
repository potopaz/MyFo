using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.BankAccounts.Commands;

public class DeleteBankAccountCommandHandler : IRequestHandler<DeleteBankAccountCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteBankAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteBankAccountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.BankAccounts
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.BankAccountId == request.BankAccountId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("BankAccount", request.BankAccountId);

        var hasMovements = await _db.MovementPayments
            .AnyAsync(mp => mp.BankAccountId == request.BankAccountId, cancellationToken);

        var hasTransfers = await _db.Transfers
            .AnyAsync(t => t.FromBankAccountId == request.BankAccountId || t.ToBankAccountId == request.BankAccountId, cancellationToken);

        var hasCCPayments = await _db.CreditCardPayments
            .AnyAsync(cp => cp.BankAccountId == request.BankAccountId, cancellationToken);

        var hasFrequentMovements = await _db.FrequentMovements
            .AnyAsync(fm => fm.BankAccountId == request.BankAccountId, cancellationToken);

        if (hasMovements || hasTransfers || hasCCPayments || hasFrequentMovements)
            throw new DomainException("BANK_ACCOUNT_IN_USE",
                "Esta cuenta bancaria ya fue utilizada en movimientos, transferencias o pagos. No se puede eliminar.");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
