using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Common.Mediator;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public class ToggleReconcileInitialBalanceCommandHandler : IRequestHandler<ToggleReconcileInitialBalanceCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleReconcileInitialBalanceCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(ToggleReconcileInitialBalanceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var bankAccount = await _db.BankAccounts
            .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == request.BankAccountId, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.BankAccountId);

        bankAccount.IsInitialBalanceReconciled = request.IsReconciled;

        await _db.SaveChangesAsync(cancellationToken);

        return bankAccount.IsInitialBalanceReconciled;
    }
}
