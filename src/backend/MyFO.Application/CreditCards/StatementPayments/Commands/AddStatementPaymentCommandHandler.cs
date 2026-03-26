using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.StatementPayments.Commands;

public class AddStatementPaymentCommandHandler : IRequestHandler<AddStatementPaymentCommand, StatementPaymentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddStatementPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StatementPaymentDto> Handle(AddStatementPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Load period with card (need currency for validation)
        var period = await _db.StatementPeriods
            .Include(sp => sp.CreditCard)
            .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                && sp.StatementPeriodId == request.StatementPeriodId, cancellationToken)
            ?? throw new NotFoundException("StatementPeriod", request.StatementPeriodId);

        // Payments allowed on periods that are not fully paid
        if (period.PaymentStatus == PaymentStatus.FullyPaid)
            throw new DomainException("PERIOD_ALREADY_PAID", "El periodo ya está pagado.");

        if (request.Amount <= 0)
            throw new DomainException("INVALID_AMOUNT", "El importe debe ser mayor a cero.");

        // IsTotalPayment only makes sense when period is closed (has a calculated total)
        if (request.IsTotalPayment && period.ClosedAt == null)
            throw new DomainException("CANNOT_TOTAL_PAY_OPEN", "No se puede hacer pago total en un periodo abierto (aún no tiene total calculado).");

        if (request.PrimaryExchangeRate <= 0)
            throw new DomainException("INVALID_EXCHANGE_RATE", "La cotización primaria debe ser mayor a cero.");
        if (request.SecondaryExchangeRate <= 0)
            throw new DomainException("INVALID_SECONDARY_EXCHANGE_RATE", "La cotización secundaria debe ser mayor a cero.");

        // Validate payment source (exactly one of CashBox or BankAccount)
        if (request.CashBoxId.HasValue == request.BankAccountId.HasValue)
            throw new DomainException("INVALID_PAYMENT_SOURCE", "Debe seleccionar una caja o un banco (no ambos).");

        if (request.CashBoxId.HasValue)
        {
            var cashBox = await _db.CashBoxes
                .FirstOrDefaultAsync(cb => cb.FamilyId == familyId && cb.CashBoxId == request.CashBoxId.Value, cancellationToken)
                ?? throw new DomainException("INVALID_CASH_BOX", "La caja no existe.");
            if (!cashBox.IsActive)
                throw new DomainException("INACTIVE_CASH_BOX", "La caja está inactiva.");

            // Update balance: paying a CC statement is an expense from the cash box
            cashBox.Balance -= request.Amount;
        }

        if (request.BankAccountId.HasValue)
        {
            var bank = await _db.BankAccounts
                .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == request.BankAccountId.Value, cancellationToken)
                ?? throw new DomainException("INVALID_BANK_ACCOUNT", "El banco no existe.");
            if (!bank.IsActive)
                throw new DomainException("INACTIVE_BANK_ACCOUNT", "El banco está inactivo.");

            bank.Balance -= request.Amount;
        }

        // If IsTotalPayment, the payment amount covers the full remaining balance
        var effectiveAmount = request.IsTotalPayment ? period.PendingBalance : request.Amount;

        // Only validate payment doesn't exceed balance when period is closed (has calculated total)
        if (period.ClosedAt != null && !request.IsTotalPayment && request.Amount > period.PendingBalance)
            throw new DomainException("PAYMENT_EXCEEDS_BALANCE", $"El pago ({request.Amount}) supera el saldo pendiente ({period.PendingBalance}).");

        var payment = new CreditCardPayment
        {
            FamilyId = familyId,
            CreditCardPaymentId = Guid.NewGuid(),
            CreditCardId = period.CreditCardId,
            StatementPeriodId = request.StatementPeriodId,
            PaymentDate = request.PaymentDate,
            Amount = effectiveAmount,
            CashBoxId = request.CashBoxId,
            BankAccountId = request.BankAccountId,
            PrimaryExchangeRate = request.PrimaryExchangeRate,
            SecondaryExchangeRate = request.SecondaryExchangeRate,
            AmountInPrimary = effectiveAmount * request.PrimaryExchangeRate,
            AmountInSecondary = effectiveAmount * request.SecondaryExchangeRate,
            IsTotalPayment = request.IsTotalPayment,
        };

        await _db.CreditCardPayments.AddAsync(payment, cancellationToken);

        // Generate pro-rata allocations only for closed periods (Open has no assigned items yet)
        if (period.ClosedAt != null)
        {
            await GenerateAllocations(familyId, period, payment, cancellationToken);
        }

        // Update period totals
        period.PaymentsTotal += effectiveAmount;

        // Only update pending balance and status for closed periods
        if (period.ClosedAt != null)
        {
            period.PendingBalance = period.StatementTotal - period.PaymentsTotal;

            if (period.PendingBalance <= 0)
                period.PaymentStatus = PaymentStatus.FullyPaid;
            else if (period.PaymentsTotal > 0)
                period.PaymentStatus = PaymentStatus.PartiallyPaid;
        }
        else
        {
            // Open period: just track payment status
            period.PaymentStatus = PaymentStatus.PartiallyPaid;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new StatementPaymentDto
        {
            StatementPaymentId = payment.CreditCardPaymentId,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            CashBoxId = payment.CashBoxId,
            BankAccountId = payment.BankAccountId,
            PrimaryExchangeRate = payment.PrimaryExchangeRate,
            SecondaryExchangeRate = payment.SecondaryExchangeRate,
            AmountInPrimary = payment.AmountInPrimary,
            AmountInSecondary = payment.AmountInSecondary,
            IsTotalPayment = payment.IsTotalPayment,
        };
    }

    /// <summary>
    /// Distributes a payment proportionally across all statement items
    /// (installments + charges - bonifications) using pro-rata proration.
    /// Each item's share = (item_amount / statement_total) * payment_amount
    /// </summary>
    private async Task GenerateAllocations(
        Guid familyId, StatementPeriod period, CreditCardPayment payment,
        CancellationToken cancellationToken)
    {
        if (period.StatementTotal <= 0) return;

        var allocations = new List<StatementPaymentAllocation>();

        // Get installments in this period
        var installments = await _db.CreditCardInstallments
            .Where(i => i.FamilyId == familyId
                && i.StatementPeriodId == period.StatementPeriodId
                && i.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Get line items in this period
        var lineItems = await _db.StatementLineItems
            .Where(li => li.FamilyId == familyId
                && li.StatementPeriodId == period.StatementPeriodId
                && li.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Build list of items with their contribution to total
        var items = new List<(Guid? installmentId, Guid? lineItemId, decimal weight)>();

        foreach (var inst in installments)
        {
            var amount = inst.ActualAmount ?? inst.EffectiveAmount;
            if (amount > 0)
                items.Add((inst.CreditCardInstallmentId, null, amount));
        }

        foreach (var li in lineItems)
        {
            if (li.LineType == StatementLineType.Charge && li.Amount > 0)
                items.Add((null, li.StatementLineItemId, li.Amount));
            else if (li.LineType == StatementLineType.Bonification && li.Amount > 0)
                items.Add((null, li.StatementLineItemId, -li.Amount));
        }

        var totalWeight = items.Sum(i => i.weight);
        if (totalWeight <= 0) return;

        var allocated = 0m;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            decimal share;

            if (i == items.Count - 1)
            {
                share = payment.Amount - allocated;
            }
            else
            {
                share = Math.Round(payment.Amount * item.weight / totalWeight, 2);
            }

            allocated += share;

            allocations.Add(new StatementPaymentAllocation
            {
                FamilyId = familyId,
                AllocationId = Guid.NewGuid(),
                CreditCardPaymentId = payment.CreditCardPaymentId,
                CreditCardInstallmentId = item.installmentId,
                StatementLineItemId = item.lineItemId,
                AmountCardCurrency = share,
                AmountInPrimary = share * payment.PrimaryExchangeRate,
                AmountInSecondary = share * payment.SecondaryExchangeRate,
                PrimaryExchangeRate = payment.PrimaryExchangeRate,
                SecondaryExchangeRate = payment.SecondaryExchangeRate,
            });
        }

        foreach (var alloc in allocations)
            await _db.StatementPaymentAllocations.AddAsync(alloc, cancellationToken);
    }
}
