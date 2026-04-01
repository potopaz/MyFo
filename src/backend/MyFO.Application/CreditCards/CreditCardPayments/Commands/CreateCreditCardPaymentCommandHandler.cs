using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.CreditCardPayments.DTOs;
using MyFO.Application.CreditCards.StatementPeriods.Commands;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.CreditCardPayments.Commands;

public class CreateCreditCardPaymentCommandHandler : IRequestHandler<CreateCreditCardPaymentCommand, CreditCardPaymentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCreditCardPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreditCardPaymentDto> Handle(CreateCreditCardPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        if (request.Amount <= 0)
            throw new DomainException("INVALID_AMOUNT", "El importe debe ser mayor a cero.");

        if (request.PrimaryExchangeRate <= 0)
            throw new DomainException("INVALID_EXCHANGE_RATE", "La cotización primaria debe ser mayor a cero.");
        if (request.SecondaryExchangeRate <= 0)
            throw new DomainException("INVALID_SECONDARY_EXCHANGE_RATE", "La cotización secundaria debe ser mayor a cero.");

        // Validate credit card exists and is active
        var creditCard = await _db.CreditCards
            .FirstOrDefaultAsync(cc => cc.FamilyId == familyId && cc.CreditCardId == request.CreditCardId, cancellationToken)
            ?? throw new DomainException("INVALID_CREDIT_CARD", "La tarjeta de crédito no existe.");
        if (!creditCard.IsActive)
            throw new DomainException("INACTIVE_CREDIT_CARD", "La tarjeta de crédito está inactiva.");

        // Validate statement period if provided
        StatementPeriod? statementPeriod = null;
        if (request.StatementPeriodId.HasValue)
        {
            statementPeriod = await _db.StatementPeriods
                .FirstOrDefaultAsync(sp => sp.FamilyId == familyId
                    && sp.StatementPeriodId == request.StatementPeriodId.Value
                    && sp.CreditCardId == request.CreditCardId, cancellationToken)
                ?? throw new DomainException("INVALID_STATEMENT_PERIOD", "El período de liquidación no existe o no corresponde a esta tarjeta.");

            if (statementPeriod.ClosedAt == null)
                throw new DomainException("PERIOD_NOT_CLOSED", "Solo se pueden asociar pagos a períodos cerrados.");

            if (statementPeriod.PaymentStatus == PaymentStatus.FullyPaid)
                throw new DomainException("PERIOD_ALREADY_PAID", "El período ya está completamente pagado.");
        }

        // IsTotalPayment requires a statement period
        if (request.IsTotalPayment && !request.StatementPeriodId.HasValue)
            throw new DomainException("TOTAL_PAYMENT_REQUIRES_PERIOD", "El pago total requiere un período de liquidación asociado.");

        // Validate payment source (exactly one)
        if (request.CashBoxId.HasValue == request.BankAccountId.HasValue)
            throw new DomainException("INVALID_PAYMENT_SOURCE", "Debe seleccionar una caja o un banco (no ambos).");

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

        var payment = new CreditCardPayment
        {
            FamilyId = familyId,
            CreditCardPaymentId = Guid.NewGuid(),
            CreditCardId = request.CreditCardId,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            Description = request.Description,
            CashBoxId = request.CashBoxId,
            BankAccountId = request.BankAccountId,
            IsTotalPayment = request.IsTotalPayment,
            StatementPeriodId = request.StatementPeriodId,
            PrimaryExchangeRate = request.PrimaryExchangeRate,
            SecondaryExchangeRate = request.SecondaryExchangeRate,
            AmountInPrimary = request.Amount * request.PrimaryExchangeRate,
            AmountInSecondary = request.Amount * request.SecondaryExchangeRate,
        };

        await _db.CreditCardPayments.AddAsync(payment, cancellationToken);

        if (statementPeriod != null)
        {
            await StatementPaymentAllocationHelper.GenerateAsync(db: _db, familyId, statementPeriod, payment, cancellationToken);
            StatementPaymentAllocationHelper.ApplyPayment(statementPeriod, payment.Amount);
        }

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
