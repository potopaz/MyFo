using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class ToggleBonificationInclusionCommandHandler : IRequestHandler<ToggleBonificationInclusionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleBonificationInclusionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ToggleBonificationInclusionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var period = await _db.StatementPeriods
            .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        if (period.ClosedAt != null)
            throw new DomainException("PERIOD_NOT_OPEN", "Solo se pueden modificar cuotas en periodos abiertos.");

        var installment = await _db.CreditCardInstallments
            .FirstOrDefaultAsync(i => i.FamilyId == familyId
                && i.CreditCardInstallmentId == request.CreditCardInstallmentId
                && i.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("CreditCardInstallment", request.CreditCardInstallmentId);

        if (installment.BonificationApplied <= 0)
            throw new DomainException("NO_BONIFICATION", "Esta cuota no tiene bonificacion.");

        if (request.Include)
        {
            if (installment.StatementPeriodId != request.StatementPeriodId)
                throw new DomainException("INSTALLMENT_NOT_INCLUDED", "Primero debe incluir la cuota en el periodo.");

            installment.ActualBonificationAmount ??= installment.BonificationApplied;
        }
        else
        {
            installment.ActualBonificationAmount = null;
        }

        // Recalculate stored totals
        await StatementPeriodTotalsHelper.Recalculate(_db, period, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
