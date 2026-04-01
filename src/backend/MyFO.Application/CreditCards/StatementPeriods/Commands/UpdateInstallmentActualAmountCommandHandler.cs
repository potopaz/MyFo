using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class UpdateInstallmentActualAmountCommandHandler : IRequestHandler<UpdateInstallmentActualAmountCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateInstallmentActualAmountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateInstallmentActualAmountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var installment = await _db.CreditCardInstallments
            .FirstOrDefaultAsync(i => i.FamilyId == familyId
                && i.CreditCardInstallmentId == request.CreditCardInstallmentId
                && i.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("CreditCardInstallment", request.CreditCardInstallmentId);

        // Can only edit if assigned to an open period
        if (installment.StatementPeriodId.HasValue)
        {
            var period = await _db.StatementPeriods
                .FirstAsync(sp => sp.StatementPeriodId == installment.StatementPeriodId.Value, cancellationToken);

            if (period.ClosedAt != null)
                throw new DomainException("PERIOD_NOT_OPEN", "Solo se puede editar el monto real de cuotas en periodos abiertos.");
        }

        if (request.ActualAmount.HasValue && request.ActualAmount.Value < 0)
            throw new DomainException("INVALID_AMOUNT", "El monto real no puede ser negativo.");

        installment.ActualAmount = request.ActualAmount;

        // Recalculate stored totals if assigned to a period
        if (installment.StatementPeriodId.HasValue)
        {
            var periodForRecalc = await _db.StatementPeriods
                .FirstAsync(sp => sp.StatementPeriodId == installment.StatementPeriodId.Value, cancellationToken);
            await StatementPeriodTotalsHelper.Recalculate(_db, periodForRecalc, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
