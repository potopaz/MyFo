using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.CreditCardPayments.DTOs;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.CreditCardPayments.Commands;

public class UpdateCreditCardPaymentCommandHandler : IRequestHandler<UpdateCreditCardPaymentCommand, CreditCardPaymentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCreditCardPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreditCardPaymentDto> Handle(UpdateCreditCardPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var payment = await _db.CreditCardPayments
            .FirstOrDefaultAsync(p => p.FamilyId == familyId && p.CreditCardPaymentId == request.CreditCardPaymentId, cancellationToken)
            ?? throw new NotFoundException("CreditCardPayment", request.CreditCardPaymentId);

        var creditCard = await _db.CreditCards
            .FirstOrDefaultAsync(cc => cc.FamilyId == familyId && cc.CreditCardId == payment.CreditCardId, cancellationToken)
            ?? throw new DomainException("INVALID_CREDIT_CARD", "La tarjeta de crédito no existe.");
        if (!creditCard.IsActive)
            throw new DomainException("INACTIVE_CREDIT_CARD", "La tarjeta de crédito está inactiva.");

        if (request.Amount <= 0)
            throw new DomainException("INVALID_AMOUNT", "El importe debe ser mayor a cero.");
        if (request.PrimaryExchangeRate <= 0)
            throw new DomainException("INVALID_EXCHANGE_RATE", "La cotización primaria debe ser mayor a cero.");
        if (request.SecondaryExchangeRate <= 0)
            throw new DomainException("INVALID_SECONDARY_EXCHANGE_RATE", "La cotización secundaria debe ser mayor a cero.");

        // Validate statement period if provided
        if (request.StatementPeriodId.HasValue)
        {
            var period = await _db.StatementPeriods
                .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                    && sp.StatementPeriodId == request.StatementPeriodId.Value
                    && sp.CreditCardId == payment.CreditCardId, cancellationToken)
                ?? throw new DomainException("INVALID_STATEMENT_PERIOD", "El período de liquidación no existe o no corresponde a esta tarjeta.");

            if (period.ClosedAt == null)
                throw new DomainException("PERIOD_NOT_CLOSED", "Solo se pueden asociar pagos a períodos cerrados.");
            if (period.PaymentStatus == PaymentStatus.FullyPaid)
                throw new DomainException("PERIOD_ALREADY_PAID", "El período ya está completamente pagado.");
        }

        if (request.IsTotalPayment && !request.StatementPeriodId.HasValue)
            throw new DomainException("TOTAL_PAYMENT_REQUIRES_PERIOD", "El pago total requiere un período de liquidación asociado.");

        if (request.CashBoxId.HasValue == request.BankAccountId.HasValue)
            throw new DomainException("INVALID_PAYMENT_SOURCE", "Debe seleccionar una caja o un banco (no ambos).");

        // --- Reverse old balance ---
        if (payment.CashBoxId.HasValue)
        {
            var oldCb = await _db.CashBoxes.FirstAsync(
                cb => cb.FamilyId == familyId && cb.CashBoxId == payment.CashBoxId.Value, cancellationToken);
            oldCb.Balance += payment.Amount;
        }
        if (payment.BankAccountId.HasValue)
        {
            var oldBa = await _db.BankAccounts.FirstAsync(
                ba => ba.FamilyId == familyId && ba.BankAccountId == payment.BankAccountId.Value, cancellationToken);
            oldBa.Balance += payment.Amount;
        }

        // --- Apply new balance ---
        string? sourceName = null;

        if (request.CashBoxId.HasValue)
        {
            var cashBox = await _db.CashBoxes
                .FirstOrDefaultAsync(cb => cb.FamilyId == familyId && cb.CashBoxId == request.CashBoxId.Value, cancellationToken)
                ?? throw new DomainException("INVALID_CASH_BOX", "La caja no existe.");
            if (!cashBox.IsActive)
                throw new DomainException("INACTIVE_CASH_BOX", "La caja está inactiva.");
            if (cashBox.CurrencyCode != creditCard.CurrencyCode)
                throw new DomainException("CURRENCY_MISMATCH", "La moneda de la caja no coincide con la moneda de la tarjeta.");
            cashBox.Balance -= request.Amount;
            sourceName = cashBox.Name;
        }

        if (request.BankAccountId.HasValue)
        {
            var bank = await _db.BankAccounts
                .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == request.BankAccountId.Value, cancellationToken)
                ?? throw new DomainException("INVALID_BANK_ACCOUNT", "El banco no existe.");
            if (!bank.IsActive)
                throw new DomainException("INACTIVE_BANK_ACCOUNT", "El banco está inactivo.");
            if (bank.CurrencyCode != creditCard.CurrencyCode)
                throw new DomainException("CURRENCY_MISMATCH", "La moneda del banco no coincide con la moneda de la tarjeta.");
            bank.Balance -= request.Amount;
            sourceName = bank.Name;
        }

        // --- Update payment ---
        payment.PaymentDate = request.PaymentDate;
        payment.Amount = request.Amount;
        payment.Description = request.Description?.Trim();
        payment.CashBoxId = request.CashBoxId;
        payment.BankAccountId = request.BankAccountId;
        payment.IsTotalPayment = request.IsTotalPayment;
        payment.StatementPeriodId = request.StatementPeriodId;
        payment.PrimaryExchangeRate = request.PrimaryExchangeRate;
        payment.SecondaryExchangeRate = request.SecondaryExchangeRate;
        payment.AmountInPrimary = request.Amount * request.PrimaryExchangeRate;
        payment.AmountInSecondary = request.Amount * request.SecondaryExchangeRate;

        await _db.SaveChangesAsync(cancellationToken);

        return new CreditCardPaymentDto
        {
            CreditCardPaymentId = payment.CreditCardPaymentId,
            CreditCardId = payment.CreditCardId,
            CreditCardName = creditCard.Name,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Description = payment.Description,
            CashBoxId = payment.CashBoxId,
            CashBoxName = request.CashBoxId.HasValue ? sourceName : null,
            BankAccountId = payment.BankAccountId,
            BankAccountName = request.BankAccountId.HasValue ? sourceName : null,
            IsTotalPayment = payment.IsTotalPayment,
            StatementPeriodId = payment.StatementPeriodId,
            PrimaryExchangeRate = payment.PrimaryExchangeRate,
            SecondaryExchangeRate = payment.SecondaryExchangeRate,
            AmountInPrimary = payment.AmountInPrimary,
            AmountInSecondary = payment.AmountInSecondary,
        };
    }
}
