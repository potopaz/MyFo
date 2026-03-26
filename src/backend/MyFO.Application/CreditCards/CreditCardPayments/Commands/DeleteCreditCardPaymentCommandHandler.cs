using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.CreditCardPayments.Commands;

public class DeleteCreditCardPaymentCommandHandler : IRequestHandler<DeleteCreditCardPaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCreditCardPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCreditCardPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var payment = await _db.CreditCardPayments
            .FirstOrDefaultAsync(p => p.FamilyId == familyId
                && p.CreditCardPaymentId == request.CreditCardPaymentId, cancellationToken)
            ?? throw new NotFoundException("CreditCardPayment", request.CreditCardPaymentId);

        // Reverse balance on source
        if (payment.CashBoxId.HasValue)
        {
            var cashBox = await _db.CashBoxes
                .FirstOrDefaultAsync(cb => cb.FamilyId == familyId && cb.CashBoxId == payment.CashBoxId.Value, cancellationToken);
            if (cashBox != null) cashBox.Balance += payment.Amount;
        }

        if (payment.BankAccountId.HasValue)
        {
            var bank = await _db.BankAccounts
                .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == payment.BankAccountId.Value, cancellationToken);
            if (bank != null) bank.Balance += payment.Amount;
        }

        // Soft delete
        payment.DeletedAt = DateTime.UtcNow;
        payment.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
