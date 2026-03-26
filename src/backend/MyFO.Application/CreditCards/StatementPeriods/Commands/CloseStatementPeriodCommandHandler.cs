using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public class CloseStatementPeriodCommandHandler : IRequestHandler<CloseStatementPeriodCommand, StatementPeriodDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CloseStatementPeriodCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StatementPeriodDto> Handle(CloseStatementPeriodCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var period = await _db.StatementPeriods
            .Include(sp => sp.CreditCard)
            .Include(sp => sp.LineItems)
            .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        if (period.ClosedAt != null)
            throw new DomainException("PERIOD_NOT_OPEN", "Solo se pueden cerrar periodos en estado Abierto.");

        // Load installments already assigned to this period (via inclusion checkboxes)
        var installments = await _db.CreditCardInstallments
            .Where(i => i.FamilyId == familyId
                && i.StatementPeriodId == period.StatementPeriodId
                && i.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Freeze totals via helper
        await StatementPeriodTotalsHelper.Recalculate(_db, period, cancellationToken);
        period.ClosedAt = DateTime.UtcNow;
        period.ClosedBy = _currentUser.UserId;

        // Update payment status
        if (period.PendingBalance <= 0)
            period.PaymentStatus = PaymentStatus.FullyPaid;
        else if (period.PaymentsTotal > 0)
            period.PaymentStatus = PaymentStatus.PartiallyPaid;

        await _db.SaveChangesAsync(cancellationToken);

        return new StatementPeriodDto
        {
            StatementPeriodId = period.StatementPeriodId,
            CreditCardId = period.CreditCardId,
            CreditCardName = period.CreditCard.Name,
            PeriodStart = period.PeriodStart,
            PeriodEnd = period.PeriodEnd,
            DueDate = period.DueDate,
            PaymentStatus = period.PaymentStatus.ToString(),
            PreviousBalance = period.PreviousBalance,
            InstallmentsTotal = period.InstallmentsTotal,
            ChargesTotal = period.ChargesTotal,
            BonificationsTotal = period.BonificationsTotal,
            StatementTotal = period.StatementTotal,
            PaymentsTotal = period.PaymentsTotal,
            PendingBalance = period.PendingBalance,
            ClosedAt = period.ClosedAt,
        };
    }
}
