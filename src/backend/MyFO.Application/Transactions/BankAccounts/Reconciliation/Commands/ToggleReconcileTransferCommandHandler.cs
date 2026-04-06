using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Common.Mediator;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public class ToggleReconcileTransferCommandHandler : IRequestHandler<ToggleReconcileTransferCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleReconcileTransferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(ToggleReconcileTransferCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var transfer = await _db.Transfers
            .FirstOrDefaultAsync(t => t.FamilyId == familyId
                && t.TransferId == request.TransferId
                && (t.FromBankAccountId == request.BankAccountId || t.ToBankAccountId == request.BankAccountId), cancellationToken)
            ?? throw new NotFoundException("Transfer", request.TransferId);

        transfer.IsReconciled = request.IsReconciled;

        await _db.SaveChangesAsync(cancellationToken);

        return transfer.IsReconciled;
    }
}
