using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPayments.Commands;

public class DeleteStatementPaymentCommandHandler : IRequestHandler<DeleteStatementPaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteStatementPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteStatementPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var payment = await _db.CreditCardPayments
            .FirstOrDefaultAsync(p => p.FamilyId == familyId
                && p.CreditCardPaymentId == request.StatementPaymentId
                && p.StatementPeriodId != null, cancellationToken)
            ?? throw new NotFoundException("CreditCardPayment", request.StatementPaymentId);

        var period = await _db.StatementPeriods
            .FirstAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == payment.StatementPeriodId!.Value, cancellationToken);

        // Can only delete payments from closed periods
        if (period.ClosedAt == null)
            throw new DomainException("PERIOD_OPEN", "No se pueden eliminar pagos de un periodo abierto.");

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        // Reverse balance on source
        if (payment.CashBoxId.HasValue)
        {
            var cashBox = await _db.CashBoxes.FirstAsync(
                cb => cb.FamilyId == familyId && cb.CashBoxId == payment.CashBoxId.Value, cancellationToken);
            cashBox.Balance += payment.Amount;
        }

        if (payment.BankAccountId.HasValue)
        {
            var bank = await _db.BankAccounts.FirstAsync(
                ba => ba.FamilyId == familyId && ba.BankAccountId == payment.BankAccountId.Value, cancellationToken);
            bank.Balance += payment.Amount;
        }

        // Soft delete allocations
        var allocations = await _db.StatementPaymentAllocations
            .Where(a => a.FamilyId == familyId
                && a.CreditCardPaymentId == payment.CreditCardPaymentId
                && a.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var alloc in allocations)
        {
            alloc.DeletedAt = now;
            alloc.DeletedBy = userId;
        }

        // Soft delete payment
        payment.DeletedAt = now;
        payment.DeletedBy = userId;

        // Recalculate period totals
        period.PaymentsTotal -= payment.Amount;
        period.PendingBalance = period.StatementTotal - period.PaymentsTotal;

        // Update payment status
        if (period.PaymentsTotal <= 0)
            period.PaymentStatus = PaymentStatus.Unpaid;
        else
            period.PaymentStatus = PaymentStatus.PartiallyPaid;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
