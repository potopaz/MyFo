using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class DeleteStatementPeriodCommandHandler : IRequestHandler<DeleteStatementPeriodCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteStatementPeriodCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteStatementPeriodCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var period = await _db.StatementPeriods
            .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        if (period.ClosedAt != null)
            throw new DomainException("PERIOD_NOT_OPEN", "Solo se pueden eliminar periodos abiertos.");

        // Unassign included installments
        var installments = await _db.CreditCardInstallments
            .Where(i => i.FamilyId == familyId
                && i.StatementPeriodId == period.StatementPeriodId
                && i.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var inst in installments)
        {
            inst.StatementPeriodId = null;
            inst.ActualAmount = null;
        }

        // Soft delete line items
        var lineItems = await _db.StatementLineItems
            .Where(li => li.FamilyId == familyId
                && li.StatementPeriodId == period.StatementPeriodId
                && li.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var li in lineItems)
        {
            li.DeletedAt = now;
            li.DeletedBy = userId;
        }

        // Soft delete the period
        period.DeletedAt = now;
        period.DeletedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
