using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.Commands;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementLineItems.Commands;

public class DeleteStatementLineItemCommandHandler : IRequestHandler<DeleteStatementLineItemCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteStatementLineItemCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteStatementLineItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var lineItem = await _db.StatementLineItems
            .Include(li => li.StatementPeriod)
            .FirstOrDefaultAsync(li => li.FamilyId == familyId
                && li.StatementLineItemId == request.StatementLineItemId, cancellationToken)
            ?? throw new NotFoundException("StatementLineItem", request.StatementLineItemId);

        if (lineItem.StatementPeriod.ClosedAt != null)
            throw new DomainException("PERIOD_NOT_OPEN", "Solo se pueden eliminar líneas de periodos abiertos.");

        lineItem.DeletedAt = DateTime.UtcNow;
        lineItem.DeletedBy = _currentUser.UserId;

        // Recalculate stored totals
        await StatementPeriodTotalsHelper.Recalculate(_db, lineItem.StatementPeriod, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
