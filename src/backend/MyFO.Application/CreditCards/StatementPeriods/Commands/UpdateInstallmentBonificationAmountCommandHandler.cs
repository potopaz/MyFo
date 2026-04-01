using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class UpdateInstallmentBonificationAmountCommandHandler : IRequestHandler<UpdateInstallmentBonificationAmountCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateInstallmentBonificationAmountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateInstallmentBonificationAmountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var installment = await _db.CreditCardInstallments
            .FirstOrDefaultAsync(i => i.FamilyId == familyId
                && i.CreditCardInstallmentId == request.CreditCardInstallmentId
                && i.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("CreditCardInstallment", request.CreditCardInstallmentId);

        if (installment.StatementPeriodId.HasValue)
        {
            var period = await _db.StatementPeriods
                .FirstAsync(sp => sp.StatementPeriodId == installment.StatementPeriodId.Value, cancellationToken);

            if (period.ClosedAt != null)
                throw new DomainException("PERIOD_NOT_OPEN", "Solo se puede editar el monto de bonificacion en periodos abiertos.");
        }

        if (installment.ActualBonificationAmount == null)
            throw new DomainException("BONIFICATION_NOT_INCLUDED", "La bonificacion no esta incluida en el resumen.");

        if (request.ActualBonificationAmount.HasValue && request.ActualBonificationAmount.Value < 0)
            throw new DomainException("INVALID_AMOUNT", "El monto de bonificacion no puede ser negativo.");

        installment.ActualBonificationAmount = request.ActualBonificationAmount;

        if (installment.StatementPeriodId.HasValue)
        {
            var periodForRecalc = await _db.StatementPeriods
                .FirstAsync(sp => sp.StatementPeriodId == installment.StatementPeriodId.Value, cancellationToken);
            await StatementPeriodTotalsHelper.Recalculate(_db, periodForRecalc, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
