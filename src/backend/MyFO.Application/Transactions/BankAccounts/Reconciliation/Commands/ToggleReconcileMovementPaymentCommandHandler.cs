using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Common.Mediator;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public class ToggleReconcileMovementPaymentCommandHandler : IRequestHandler<ToggleReconcileMovementPaymentCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleReconcileMovementPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(ToggleReconcileMovementPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var payment = await _db.MovementPayments
            .FirstOrDefaultAsync(mp => mp.FamilyId == familyId
                && mp.MovementPaymentId == request.MovementPaymentId
                && mp.BankAccountId == request.BankAccountId, cancellationToken)
            ?? throw new NotFoundException("MovementPayment", request.MovementPaymentId);

        payment.IsReconciled = request.IsReconciled;

        await _db.SaveChangesAsync(cancellationToken);

        return payment.IsReconciled;
    }
}
