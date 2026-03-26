using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;
namespace MyFO.Application.Transactions.Transfers.Commands;

public class ConfirmTransferCommandHandler : IRequestHandler<ConfirmTransferCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ConfirmTransferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ConfirmTransferCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var transfer = await _db.Transfers
            .FirstOrDefaultAsync(t => t.FamilyId == familyId && t.TransferId == request.TransferId, cancellationToken)
            ?? throw new NotFoundException("Transfer", request.TransferId);

        if (transfer.Status != TransferStatus.PendingConfirmation)
            throw new DomainException("INVALID_STATUS", "Solo se pueden confirmar transferencias en estado pendiente.");

        // Check that current user has Operate permission on the destination
        var canOperate = await CanOperateDestinationAsync(transfer, familyId, cancellationToken);
        if (!canOperate)
            throw new ForbiddenException("No tiene permisos para confirmar esta transferencia.");

        // Apply balance side effects
        if (transfer.FromCashBoxId.HasValue)
        {
            var fromCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.FromCashBoxId.Value, cancellationToken);
            fromCb.Balance -= transfer.Amount;
        }
        else if (transfer.FromBankAccountId.HasValue)
        {
            var fromBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.FromBankAccountId.Value, cancellationToken);
            fromBa.Balance -= transfer.Amount;
        }

        if (transfer.ToCashBoxId.HasValue)
        {
            var toCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.ToCashBoxId.Value, cancellationToken);
            toCb.Balance += transfer.AmountTo;
        }
        else if (transfer.ToBankAccountId.HasValue)
        {
            var toBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.ToBankAccountId.Value, cancellationToken);
            toBa.Balance += transfer.AmountTo;
        }

        transfer.Status = TransferStatus.Confirmed;
        transfer.IsAutoConfirmed = false;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> CanOperateDestinationAsync(Domain.Transactions.Transfer transfer, Guid familyId, CancellationToken cancellationToken)
    {
        // BankAccount destination: always can operate
        if (transfer.ToBankAccountId.HasValue)
            return true;

        // CashBox destination: mirrors GetCashBoxesQueryHandler CanOperate logic
        if (transfer.ToCashBoxId.HasValue)
        {
            var member = await _db.FamilyMembers
                .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId, cancellationToken);

            if (member == null)
                return false;

            var permission = await _db.CashBoxPermissions
                .FirstOrDefaultAsync(p => p.CashBoxId == transfer.ToCashBoxId.Value && p.MemberId == member.MemberId, cancellationToken);

            // Record exists (non-deleted) = has permission to operate
            return permission != null;
        }

        return false;
    }
}
