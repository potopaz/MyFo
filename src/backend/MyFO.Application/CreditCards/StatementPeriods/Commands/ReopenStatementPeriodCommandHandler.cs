using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class ReopenStatementPeriodCommandHandler : IRequestHandler<ReopenStatementPeriodCommand, StatementPeriodDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ReopenStatementPeriodCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StatementPeriodDto> Handle(ReopenStatementPeriodCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var period = await _db.StatementPeriods
            .Include(sp => sp.CreditCard)
            .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        if (period.ClosedAt == null)
            throw new DomainException("PERIOD_NOT_CLOSED", "Solo se pueden reabrir periodos cerrados.");

        if (period.PaymentStatus != PaymentStatus.Unpaid)
            throw new DomainException("PERIOD_HAS_PAYMENTS", "No se puede reabrir un periodo con pagos registrados.");

        // Check no subsequent closed period exists
        var hasNextPeriod = await _db.StatementPeriods
            .AnyAsync(sp => sp.FamilyId == familyId
                && sp.CreditCardId == period.CreditCardId
                && sp.PeriodEnd > period.PeriodEnd
                && sp.ClosedAt != null, cancellationToken);

        if (hasNextPeriod)
            throw new DomainException("NEXT_PERIOD_CLOSED", "No se puede reabrir porque existe un periodo posterior ya cerrado.");

        // Keep installments assigned (user already curated the selection)
        // Reset frozen totals — they will be calculated dynamically while open
        period.PaymentStatus = PaymentStatus.Unpaid;
        period.InstallmentsTotal = 0;
        period.ChargesTotal = 0;
        period.BonificationsTotal = 0;
        period.StatementTotal = 0;
        period.ClosedAt = null;
        period.ClosedBy = null;

        await _db.SaveChangesAsync(cancellationToken);

        return new StatementPeriodDto
        {
            StatementPeriodId = period.StatementPeriodId,
            CreditCardId = period.CreditCardId,
            CreditCardName = period.CreditCard.Name,
            PeriodEnd = period.PeriodEnd,
            DueDate = period.DueDate,
            PaymentStatus = period.PaymentStatus.ToString(),
            PreviousBalance = period.PreviousBalance,
            ClosedAt = period.ClosedAt,
        };
    }
}
