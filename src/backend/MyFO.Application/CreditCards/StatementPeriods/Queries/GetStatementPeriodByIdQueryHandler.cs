using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Domain.Exceptions;

namespace MyFO.Application.CreditCards.StatementPeriods.Queries;

public class GetStatementPeriodByIdQueryHandler : IRequestHandler<GetStatementPeriodByIdQuery, StatementPeriodDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetStatementPeriodByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<StatementPeriodDetailDto> Handle(GetStatementPeriodByIdQuery request, CancellationToken cancellationToken)
    {
        var period = await _db.StatementPeriods
            .Include(sp => sp.CreditCard)
            .Include(sp => sp.LineItems)
            .FirstOrDefaultAsync(sp => sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        var isOpen = period.ClosedAt == null;
        var periodId = period.StatementPeriodId;

        // When open: assigned (included) + unassigned candidates
        // When closed: only assigned
        var installmentsQuery = _db.CreditCardInstallments
            .Where(i => i.DeletedAt == null);

        if (isOpen)
        {
            installmentsQuery = installmentsQuery
                .Where(i =>
                    i.StatementPeriodId == periodId
                    || (i.StatementPeriodId == null && i.EstimatedDate <= period.PeriodEnd));
        }
        else
        {
            installmentsQuery = installmentsQuery
                .Where(i => i.StatementPeriodId == periodId);
        }

        // For open periods, among unassigned candidates keep only the first per payment
        // (lowest InstallmentNumber) to avoid showing all future installments of a purchase
        List<Guid>? firstUnassignedIds = null;
        if (isOpen)
        {
            firstUnassignedIds = await _db.CreditCardInstallments
                .Where(i => i.DeletedAt == null && i.StatementPeriodId == null && i.EstimatedDate <= period.PeriodEnd)
                .Join(_db.MovementPayments,
                    i => new { i.FamilyId, i.MovementPaymentId },
                    mp => new { mp.FamilyId, mp.MovementPaymentId },
                    (i, mp) => new { i.CreditCardInstallmentId, i.MovementPaymentId, i.FamilyId, i.InstallmentNumber, mp.CreditCardId })
                .Where(x => x.CreditCardId == period.CreditCardId)
                .GroupBy(x => new { x.FamilyId, x.MovementPaymentId })
                .Select(g => g.OrderBy(x => x.InstallmentNumber).First().CreditCardInstallmentId)
                .ToListAsync(cancellationToken);

            // Re-filter: assigned OR in the first-unassigned set
            installmentsQuery = _db.CreditCardInstallments
                .Where(i => i.DeletedAt == null)
                .Where(i =>
                    i.StatementPeriodId == periodId
                    || (i.StatementPeriodId == null && firstUnassignedIds.Contains(i.CreditCardInstallmentId)));
        }

        var installments = await installmentsQuery
            .Join(_db.MovementPayments,
                i => new { i.FamilyId, i.MovementPaymentId },
                mp => new { mp.FamilyId, mp.MovementPaymentId },
                (i, mp) => new { Installment = i, Payment = mp })
            .Where(x => x.Payment.CreditCardId == period.CreditCardId)
            .Join(_db.Movements,
                x => new { x.Payment.FamilyId, x.Payment.MovementId },
                m => new { m.FamilyId, m.MovementId },
                (x, m) => new { x.Installment, x.Payment, Movement = m })
            .GroupJoin(_db.CreditCardMembers,
                x => new { x.Payment.FamilyId, CreditCardMemberId = x.Payment.CreditCardMemberId ?? Guid.Empty },
                cm => new { cm.FamilyId, cm.CreditCardMemberId },
                (x, members) => new { x.Installment, x.Payment, x.Movement, Members = members })
            .SelectMany(x => x.Members.DefaultIfEmpty(),
                (x, member) => new { x.Installment, x.Payment, x.Movement, Member = member })
            .OrderBy(x => x.Member != null ? x.Member.HolderName : "")
            .ThenBy(x => x.Installment.EstimatedDate)
            .ThenBy(x => x.Movement.Date)
            .Select(x => new StatementInstallmentDto
            {
                CreditCardInstallmentId = x.Installment.CreditCardInstallmentId,
                MovementPaymentId = x.Installment.MovementPaymentId,
                InstallmentNumber = x.Installment.InstallmentNumber,
                ProjectedAmount = x.Installment.ProjectedAmount,
                BonificationApplied = x.Installment.BonificationApplied,
                EffectiveAmount = x.Installment.EffectiveAmount,
                ActualAmount = x.Installment.ActualAmount,
                EstimatedDate = x.Installment.EstimatedDate,
                MovementDescription = x.Movement.Description,
                MovementDate = x.Movement.Date,
                TotalInstallments = x.Payment.Installments,
                IsIncluded = x.Installment.StatementPeriodId == periodId,
                ActualBonificationAmount = x.Installment.ActualBonificationAmount,
                IsBonificationIncluded = x.Installment.ActualBonificationAmount != null,
                CreditCardMemberName = x.Member != null ? x.Member.HolderName : null,
            })
            .ToListAsync(cancellationToken);

        var lineItems = period.LineItems
            .Where(li => li.DeletedAt == null)
            .OrderBy(li => li.LineType.ToString())
            .ThenBy(li => li.CreatedAt)
            .Select(li => new StatementLineItemDto
            {
                StatementLineItemId = li.StatementLineItemId,
                LineType = li.LineType.ToString(),
                Description = li.Description,
                Amount = li.Amount,
            })
            .ToList();

        // Calculate totals dynamically when open (only included installments), stored when closed
        decimal installmentsTotal, chargesTotal, bonificationsTotal, statementTotal, pendingBalance;

        if (isOpen)
        {
            var included = installments.Where(i => i.IsIncluded).ToList();
            installmentsTotal = included.Sum(i => i.ActualAmount ?? 0m);
            var installmentBonifications = included.Sum(i => i.ActualBonificationAmount ?? 0m);
            chargesTotal = lineItems.Where(li => li.LineType == "Charge").Sum(li => li.Amount);
            var lineItemBonifications = lineItems.Where(li => li.LineType == "Bonification").Sum(li => li.Amount);
            bonificationsTotal = installmentBonifications + lineItemBonifications;
            statementTotal = period.PreviousBalance + installmentsTotal + chargesTotal - bonificationsTotal;
            pendingBalance = statementTotal - period.PaymentsTotal;
        }
        else
        {
            installmentsTotal = period.InstallmentsTotal;
            chargesTotal = period.ChargesTotal;
            bonificationsTotal = period.BonificationsTotal;
            statementTotal = period.StatementTotal;
            pendingBalance = period.PendingBalance;
        }

        return new StatementPeriodDetailDto
        {
            StatementPeriodId = period.StatementPeriodId,
            CreditCardId = period.CreditCardId,
            CreditCardName = period.CreditCard.Name,
            PeriodStart = period.PeriodStart,
            PeriodEnd = period.PeriodEnd,
            DueDate = period.DueDate,
            PaymentStatus = period.PaymentStatus.ToString(),
            PreviousBalance = period.PreviousBalance,
            InstallmentsTotal = installmentsTotal,
            ChargesTotal = chargesTotal,
            BonificationsTotal = bonificationsTotal,
            StatementTotal = statementTotal,
            PaymentsTotal = period.PaymentsTotal,
            PendingBalance = pendingBalance,
            ClosedAt = period.ClosedAt,
            Installments = installments,
            LineItems = lineItems,
        };
    }
}
