using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.Commands;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementLineItems.Commands;

public class AddStatementLineItemCommandHandler : IRequestHandler<AddStatementLineItemCommand, StatementLineItemDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddStatementLineItemCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StatementLineItemDto> Handle(AddStatementLineItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var period = await _db.StatementPeriods
            .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        // Line items can be added to Open periods (before close)
        if (period.ClosedAt != null)
            throw new DomainException("PERIOD_NOT_OPEN", "Solo se pueden agregar líneas a periodos abiertos.");

        if (!Enum.TryParse<StatementLineType>(request.LineType, true, out var lineType))
            throw new DomainException("INVALID_LINE_TYPE", "Tipo de línea inválido. Valores: Charge, Bonification.");

        if (request.Amount <= 0)
            throw new DomainException("INVALID_AMOUNT", "El importe debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new DomainException("MISSING_DESCRIPTION", "La descripción es requerida.");

        var lineItem = new StatementLineItem
        {
            FamilyId = familyId,
            StatementLineItemId = Guid.NewGuid(),
            StatementPeriodId = request.StatementPeriodId,
            LineType = lineType,
            Description = request.Description.Trim(),
            Amount = request.Amount,
        };

        await _db.StatementLineItems.AddAsync(lineItem, cancellationToken);

        // Recalculate stored totals
        await StatementPeriodTotalsHelper.Recalculate(_db, period, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new StatementLineItemDto
        {
            StatementLineItemId = lineItem.StatementLineItemId,
            LineType = lineItem.LineType.ToString(),
            Description = lineItem.Description,
            Amount = lineItem.Amount,
        };
    }
}
