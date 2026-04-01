using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;

namespace MyFO.Application.CreditCards.StatementPeriods.Commands;

public static class StatementPaymentAllocationHelper
{
    /// <summary>
    /// Distributes a payment proportionally across all statement items.
    /// Items: installment gross, installment bonification (negative), line item charge, line item bonification (negative).
    /// Installment bonification rows have movement_payment_id set to trace the source purchase.
    /// Only call for closed periods.
    /// </summary>
    public static async Task GenerateAsync(
        IApplicationDbContext db, Guid familyId,
        StatementPeriod period, CreditCardPayment payment,
        CancellationToken ct)
    {
        if (period.StatementTotal <= 0) return;

        var installments = await db.CreditCardInstallments
            .Where(i => i.FamilyId == familyId
                && i.StatementPeriodId == period.StatementPeriodId
                && i.DeletedAt == null)
            .ToListAsync(ct);

        var lineItems = await db.StatementLineItems
            .Where(li => li.FamilyId == familyId
                && li.StatementPeriodId == period.StatementPeriodId
                && li.DeletedAt == null)
            .ToListAsync(ct);

        // (installmentId, movementPaymentId, lineItemId, weight)
        var items = new List<(Guid? installmentId, Guid? movementPaymentId, Guid? lineItemId, decimal weight)>();

        foreach (var inst in installments)
        {
            var amount = inst.ActualAmount ?? inst.EffectiveAmount;
            if (amount > 0)
                items.Add((inst.CreditCardInstallmentId, null, null, amount));

            // Separate bonification row — negative weight, traces back to source movement
            var bonif = inst.ActualBonificationAmount;
            if (bonif.HasValue && bonif.Value > 0)
                items.Add((inst.CreditCardInstallmentId, inst.MovementPaymentId, null, -bonif.Value));
        }

        foreach (var li in lineItems)
        {
            if (li.LineType == StatementLineType.Charge && li.Amount > 0)
                items.Add((null, null, li.StatementLineItemId, li.Amount));
            else if (li.LineType == StatementLineType.Bonification && li.Amount > 0)
                items.Add((null, null, li.StatementLineItemId, -li.Amount));
        }

        var totalWeight = items.Sum(i => i.weight);
        if (totalWeight <= 0) return;

        var allocated = 0m;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            decimal share = i == items.Count - 1
                ? payment.Amount - allocated
                : Math.Round(payment.Amount * item.weight / totalWeight, 2);

            allocated += share;

            await db.StatementPaymentAllocations.AddAsync(new StatementPaymentAllocation
            {
                FamilyId = familyId,
                AllocationId = Guid.NewGuid(),
                CreditCardPaymentId = payment.CreditCardPaymentId,
                CreditCardInstallmentId = item.installmentId,
                MovementPaymentId = item.movementPaymentId,
                StatementLineItemId = item.lineItemId,
                AmountCardCurrency = share,
                AmountInPrimary = share * payment.PrimaryExchangeRate,
                AmountInSecondary = share * payment.SecondaryExchangeRate,
                PrimaryExchangeRate = payment.PrimaryExchangeRate,
                SecondaryExchangeRate = payment.SecondaryExchangeRate,
            }, ct);
        }
    }

    /// <summary>
    /// Soft-deletes all allocations for the given payment.
    /// </summary>
    public static async Task DeleteAsync(
        IApplicationDbContext db, Guid familyId,
        Guid paymentId, Guid? userId,
        CancellationToken ct)
    {
        var allocations = await db.StatementPaymentAllocations
            .Where(a => a.FamilyId == familyId
                && a.CreditCardPaymentId == paymentId
                && a.DeletedAt == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var alloc in allocations)
        {
            alloc.DeletedAt = now;
            alloc.DeletedBy = userId;
        }
    }

    /// <summary>
    /// Updates period totals after adding a payment.
    /// </summary>
    public static void ApplyPayment(StatementPeriod period, decimal amount)
    {
        period.PaymentsTotal += amount;
        period.PendingBalance = period.StatementTotal - period.PaymentsTotal;

        if (period.PendingBalance <= 0)
            period.PaymentStatus = PaymentStatus.FullyPaid;
        else
            period.PaymentStatus = PaymentStatus.PartiallyPaid;
    }

    /// <summary>
    /// Reverses period totals after removing a payment.
    /// </summary>
    public static void ReversePayment(StatementPeriod period, decimal amount)
    {
        period.PaymentsTotal -= amount;
        period.PendingBalance = period.StatementTotal - period.PaymentsTotal;

        if (period.PaymentsTotal <= 0)
            period.PaymentStatus = PaymentStatus.Unpaid;
        else
            period.PaymentStatus = PaymentStatus.PartiallyPaid;
    }
}
