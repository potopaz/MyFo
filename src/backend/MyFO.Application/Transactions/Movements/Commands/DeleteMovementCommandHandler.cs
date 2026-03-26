using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Movements.Commands;

public class DeleteMovementCommandHandler : IRequestHandler<DeleteMovementCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteMovementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteMovementCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var movement = await _db.Movements
            .Include(m => m.Payments)
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.MovementId == request.MovementId, cancellationToken)
            ?? throw new NotFoundException("Movement", request.MovementId);

        // Check if any CC payments have installments in closed statements
        var ccPaymentIds = movement.Payments
            .Where(p => p.PaymentMethodType == PaymentMethodType.CreditCard)
            .Select(p => p.MovementPaymentId)
            .ToList();

        if (ccPaymentIds.Count > 0)
        {
            var hasClosedInstallments = await _db.CreditCardInstallments
                .AnyAsync(i => ccPaymentIds.Contains(i.MovementPaymentId)
                    && i.StatementPeriodId != null
                    && i.DeletedAt == null, cancellationToken);

            if (hasClosedInstallments)
                throw new DomainException("CC_PAYMENT_IN_STATEMENT",
                    "No se puede eliminar el movimiento porque tiene cuotas incluidas en un resumen de tarjeta.");
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        // Soft delete installments
        if (ccPaymentIds.Count > 0)
        {
            var installments = await _db.CreditCardInstallments
                .Where(i => ccPaymentIds.Contains(i.MovementPaymentId) && i.DeletedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var inst in installments)
            {
                inst.DeletedAt = now;
                inst.DeletedBy = userId;
            }
        }

        // Reverse balances
        var sign = movement.MovementType == MovementType.Income ? 1 : -1;
        foreach (var payment in movement.Payments)
        {
            switch (payment.PaymentMethodType)
            {
                case PaymentMethodType.CashBox:
                    var cb = await _db.CashBoxes.FirstAsync(
                        x => x.FamilyId == familyId && x.CashBoxId == payment.CashBoxId!.Value, cancellationToken);
                    cb.Balance -= sign * payment.Amount;
                    break;
                case PaymentMethodType.BankAccount:
                    var ba = await _db.BankAccounts.FirstAsync(
                        x => x.FamilyId == familyId && x.BankAccountId == payment.BankAccountId!.Value, cancellationToken);
                    ba.Balance -= sign * payment.Amount;
                    break;
            }

            // Soft delete payment
            payment.DeletedAt = now;
            payment.DeletedBy = userId;
        }

        // Soft delete movement
        movement.DeletedAt = now;
        movement.DeletedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
