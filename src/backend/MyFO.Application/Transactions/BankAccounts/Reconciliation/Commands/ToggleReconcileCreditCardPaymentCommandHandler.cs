using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Common.Mediator;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public class ToggleReconcileCreditCardPaymentCommandHandler : IRequestHandler<ToggleReconcileCreditCardPaymentCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleReconcileCreditCardPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(ToggleReconcileCreditCardPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var payment = await _db.CreditCardPayments
            .FirstOrDefaultAsync(cp => cp.FamilyId == familyId
                && cp.CreditCardPaymentId == request.CreditCardPaymentId
                && cp.BankAccountId == request.BankAccountId, cancellationToken)
            ?? throw new NotFoundException("CreditCardPayment", request.CreditCardPaymentId);

        payment.IsReconciled = request.IsReconciled;

        await _db.SaveChangesAsync(cancellationToken);

        return payment.IsReconciled;
    }
}
