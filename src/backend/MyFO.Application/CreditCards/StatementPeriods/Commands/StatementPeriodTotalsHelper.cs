using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public static class StatementPeriodTotalsHelper
{
    public static async Task Recalculate(IApplicationDbContext db, StatementPeriod period, CancellationToken ct)
    {
        var installments = await db.CreditCardInstallments
            .Where(i => i.FamilyId == period.FamilyId
                && i.StatementPeriodId == period.StatementPeriodId
                && i.DeletedAt == null)
            .ToListAsync(ct);

        var installmentsTotal = installments.Sum(i => i.ActualAmount ?? 0m);
        var installmentBonifications = installments.Sum(i => i.ActualBonificationAmount ?? 0m);

        var lineItems = await db.StatementLineItems
            .Where(li => li.FamilyId == period.FamilyId
                && li.StatementPeriodId == period.StatementPeriodId
                && li.DeletedAt == null)
            .ToListAsync(ct);

        var chargesTotal = lineItems
            .Where(li => li.LineType == StatementLineType.Charge)
            .Sum(li => li.Amount);

        var lineItemBonifications = lineItems
            .Where(li => li.LineType == StatementLineType.Bonification)
            .Sum(li => li.Amount);

        period.InstallmentsTotal = installmentsTotal;
        period.ChargesTotal = chargesTotal;
        period.BonificationsTotal = installmentBonifications + lineItemBonifications;
        period.StatementTotal = period.PreviousBalance + installmentsTotal + chargesTotal - period.BonificationsTotal;
        period.PendingBalance = period.StatementTotal - period.PaymentsTotal;
    }
}
