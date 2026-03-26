using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class ToggleInstallmentInclusionCommandHandler : IRequestHandler<ToggleInstallmentInclusionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ToggleInstallmentInclusionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(ToggleInstallmentInclusionCommand request, CancellationToken cancellationToken)
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

        if (request.Include)
        {
            if (installment.StatementPeriodId.HasValue && installment.StatementPeriodId != request.StatementPeriodId)
                throw new DomainException("INSTALLMENT_ASSIGNED", "La cuota ya esta asignada a otro periodo.");

            installment.StatementPeriodId = request.StatementPeriodId;
            installment.ActualAmount ??= installment.ProjectedAmount;
        }
        else
        {
            if (installment.StatementPeriodId != request.StatementPeriodId)
                throw new DomainException("INSTALLMENT_NOT_IN_PERIOD", "La cuota no pertenece a este periodo.");

            installment.StatementPeriodId = null;
            installment.ActualAmount = null;
            installment.ActualBonificationAmount = null;
        }

        // Recalculate stored totals
        await StatementPeriodTotalsHelper.Recalculate(_db, period, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
