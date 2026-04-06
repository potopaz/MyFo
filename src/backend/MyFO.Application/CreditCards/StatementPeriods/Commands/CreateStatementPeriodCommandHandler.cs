using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class CreateStatementPeriodCommandHandler : IRequestHandler<CreateStatementPeriodCommand, StatementPeriodDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateStatementPeriodCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StatementPeriodDto> Handle(CreateStatementPeriodCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Validate credit card exists
        var card = await _db.CreditCards
            .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.CreditCardId == request.CreditCardId, cancellationToken)
            ?? throw new NotFoundException("CreditCard", request.CreditCardId);

        // Validate due date
        if (request.DueDate < request.PeriodEnd)
            throw new DomainException("INVALID_DUE_DATE", "La fecha de vencimiento debe ser igual o posterior al cierre.");

        // Check no open period exists for this card
        var hasOpenPeriod = await _db.StatementPeriods
            .AnyAsync(sp => sp.FamilyId == familyId
                && sp.CreditCardId == request.CreditCardId
                && sp.ClosedAt == null, cancellationToken);

        if (hasOpenPeriod)
            throw new DomainException("OPEN_PERIOD_EXISTS", "Ya existe un periodo abierto para esta tarjeta. Cierre el periodo actual antes de crear uno nuevo.");

        // Check no duplicate period end date
        var hasDuplicate = await _db.StatementPeriods
            .AnyAsync(sp => sp.FamilyId == familyId
                && sp.CreditCardId == request.CreditCardId
                && sp.PeriodEnd == request.PeriodEnd, cancellationToken);

        if (hasDuplicate)
            throw new DomainException("PERIOD_OVERLAP", "Ya existe un periodo con esa fecha de cierre.");

        // Get previous period's pending balance (if any)
        var previousPeriod = await _db.StatementPeriods
            .Where(sp => sp.FamilyId == familyId
                && sp.CreditCardId == request.CreditCardId
                && sp.PeriodEnd < request.PeriodEnd)
            .OrderByDescending(sp => sp.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        var previousBalance = previousPeriod?.PendingBalance ?? 0m;

        var period = new StatementPeriod
        {
            FamilyId = familyId,
            StatementPeriodId = Guid.NewGuid(),
            CreditCardId = request.CreditCardId,
            PeriodEnd = request.PeriodEnd,
            DueDate = request.DueDate,
            PaymentStatus = PaymentStatus.Unpaid,
            PreviousBalance = previousBalance,
        };

        await _db.StatementPeriods.AddAsync(period, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(period, card.Name);
    }

    private static StatementPeriodDto MapToDto(StatementPeriod period, string cardName) => new()
    {
        StatementPeriodId = period.StatementPeriodId,
        CreditCardId = period.CreditCardId,
        CreditCardName = cardName,
        PeriodEnd = period.PeriodEnd,
        DueDate = period.DueDate,
        PaymentStatus = period.PaymentStatus.ToString(),
        PreviousBalance = period.PreviousBalance,
        ClosedAt = period.ClosedAt,
    };
}
