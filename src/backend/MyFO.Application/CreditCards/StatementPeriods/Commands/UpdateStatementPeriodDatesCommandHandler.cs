using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class UpdateStatementPeriodDatesCommandHandler : IRequestHandler<UpdateStatementPeriodDatesCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateStatementPeriodDatesCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateStatementPeriodDatesCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var period = await _db.StatementPeriods
            .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        if (period.ClosedAt != null)
            throw new DomainException("PERIOD_NOT_OPEN", "Solo se pueden editar periodos abiertos.");

        if (request.DueDate < request.PeriodEnd)
            throw new DomainException("INVALID_DUE_DATE", "La fecha de vencimiento debe ser igual o posterior al cierre.");

        // Check no duplicate period end date (excluding self)
        var hasDuplicate = await _db.StatementPeriods
            .AnyAsync(sp => sp.FamilyId == familyId
                && sp.CreditCardId == period.CreditCardId
                && sp.StatementPeriodId != period.StatementPeriodId
                && sp.PeriodEnd == request.PeriodEnd, cancellationToken);

        if (hasDuplicate)
            throw new DomainException("PERIOD_OVERLAP", "Ya existe un periodo con esa fecha de cierre.");

        period.PeriodEnd = request.PeriodEnd;
        period.DueDate = request.DueDate;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
