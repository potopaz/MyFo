using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Transfers.Commands;

public class DeleteTransferCommandHandler : IRequestHandler<DeleteTransferCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteTransferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteTransferCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var transfer = await _db.Transfers
            .FirstOrDefaultAsync(t => t.FamilyId == familyId && t.TransferId == request.TransferId, cancellationToken)
            ?? throw new NotFoundException("Transfer", request.TransferId);

        // Apply state-based rules:
        // - PendingConfirmation: only creator can delete (no balance reversal needed)
        // - Confirmed + IsAutoConfirmed: allowed, reverse balance
        // - Confirmed + !IsAutoConfirmed (manual): NOT allowed
        // - Rejected: NOT allowed
        if (transfer.Status == TransferStatus.PendingConfirmation)
        {
            if (transfer.CreatedBy != _currentUser.UserId)
                throw new ForbiddenException("Solo el creador puede eliminar una transferencia pendiente.");
        }
        else if (transfer.Status == TransferStatus.Confirmed && transfer.IsAutoConfirmed)
        {
            // Allowed — will reverse balance below
        }
        else
        {
            throw new DomainException("CANNOT_DELETE_TRANSFER", "Esta transferencia no puede eliminarse en su estado actual.");
        }

        // Reverse side effects only if confirmed (balance was already applied)
        if (transfer.Status == TransferStatus.Confirmed)
        {
            if (transfer.FromCashBoxId.HasValue)
            {
                var fromCb = await _db.CashBoxes.FirstAsync(
                    x => x.FamilyId == familyId && x.CashBoxId == transfer.FromCashBoxId.Value, cancellationToken);
                fromCb.Balance += transfer.Amount;
            }
            else if (transfer.FromBankAccountId.HasValue)
            {
                var fromBa = await _db.BankAccounts.FirstAsync(
                    x => x.FamilyId == familyId && x.BankAccountId == transfer.FromBankAccountId.Value, cancellationToken);
                fromBa.Balance += transfer.Amount;
            }

            if (transfer.ToCashBoxId.HasValue)
            {
                var toCb = await _db.CashBoxes.FirstAsync(
                    x => x.FamilyId == familyId && x.CashBoxId == transfer.ToCashBoxId.Value, cancellationToken);
                toCb.Balance -= transfer.AmountTo;
            }
            else if (transfer.ToBankAccountId.HasValue)
            {
                var toBa = await _db.BankAccounts.FirstAsync(
                    x => x.FamilyId == familyId && x.BankAccountId == transfer.ToBankAccountId.Value, cancellationToken);
                toBa.Balance -= transfer.AmountTo;
            }
        }

        // Soft delete
        transfer.DeletedAt = DateTime.UtcNow;
        transfer.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
