using MediatR;
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

        if (request.PeriodEnd <= period.PeriodStart)
            throw new DomainException("INVALID_PERIOD_DATES", "La fecha de cierre debe ser posterior a la de inicio.");

        if (request.DueDate < request.PeriodEnd)
            throw new DomainException("INVALID_DUE_DATE", "La fecha de vencimiento debe ser igual o posterior al cierre.");

        // Check for overlapping periods (excluding self)
        var hasOverlap = await _db.StatementPeriods
            .AnyAsync(sp => sp.FamilyId == familyId
                && sp.CreditCardId == period.CreditCardId
                && sp.StatementPeriodId != period.StatementPeriodId
                && sp.PeriodStart <= request.PeriodEnd
                && sp.PeriodEnd >= period.PeriodStart, cancellationToken);

        if (hasOverlap)
            throw new DomainException("PERIOD_OVERLAP", "El periodo se superpone con uno existente.");

        period.PeriodEnd = request.PeriodEnd;
        period.DueDate = request.DueDate;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
