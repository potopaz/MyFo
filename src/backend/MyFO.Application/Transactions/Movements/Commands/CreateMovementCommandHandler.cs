using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.Movements.DTOs;
using MyFO.Domain.Accounting.Enums;
using MyFO.Domain.CreditCards;
using MyFO.Domain.CreditCards.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Movements.Commands;

public class CreateMovementCommandHandler : IRequestHandler<CreateMovementCommand, MovementDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateMovementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MovementDto> Handle(CreateMovementCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate family
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // 2. Load family settings
        var family = await _db.Families
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.FamilyId == familyId && f.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("Family", familyId);

        // 3. Amount > 0
        if (request.Amount <= 0)
            throw new DomainException("INVALID_AMOUNT", "El importe debe ser mayor a cero.");

        // 4. CurrencyCode must be active FamilyCurrency
        var familyCurrency = await _db.FamilyCurrencies
            .FirstOrDefaultAsync(fc => fc.FamilyId == familyId
                && fc.Currency.Code == request.CurrencyCode.ToUpperInvariant(), cancellationToken);
        if (familyCurrency is null)
            throw new DomainException("INVALID_CURRENCY", $"La moneda '{request.CurrencyCode}' no está asociada a la familia.");
        if (!familyCurrency.IsActive)
            throw new DomainException("INACTIVE_CURRENCY", $"La moneda '{request.CurrencyCode}' está inactiva.");

        // 5. Subcategory must exist and be active
        var subcategory = await _db.Subcategories
            .FirstOrDefaultAsync(s => s.FamilyId == familyId && s.SubcategoryId == request.SubcategoryId, cancellationToken)
            ?? throw new DomainException("INVALID_SUBCATEGORY", "La subcategoría no existe.");
        if (!subcategory.IsActive)
            throw new DomainException("INACTIVE_SUBCATEGORY", "La subcategoría está inactiva.");

        // 6 & 7. MovementType resolution
        MovementType movementType;
        if (subcategory.SubcategoryType == SubcategoryType.Both)
        {
            if (string.IsNullOrWhiteSpace(request.MovementType))
                throw new DomainException("MOVEMENT_TYPE_REQUIRED", "El tipo de movimiento es requerido para subcategorías de tipo 'Ambos'.");
            if (!Enum.TryParse<MovementType>(request.MovementType, true, out movementType))
                throw new DomainException("INVALID_MOVEMENT_TYPE", "Tipo de movimiento inválido. Valores: Income, Expense.");
        }
        else
        {
            movementType = subcategory.SubcategoryType == SubcategoryType.Income
                ? MovementType.Income
                : MovementType.Expense;
        }

        // 8. CostCenter validation
        if (request.CostCenterId.HasValue)
        {
            var costCenter = await _db.CostCenters
                .FirstOrDefaultAsync(cc => cc.FamilyId == familyId && cc.CostCenterId == request.CostCenterId.Value, cancellationToken)
                ?? throw new DomainException("INVALID_COST_CENTER", "El centro de costo no existe.");
            if (!costCenter.IsActive)
                throw new DomainException("INACTIVE_COST_CENTER", "El centro de costo está inactivo.");
        }

        // 9. PrimaryExchangeRate > 0
        if (request.PrimaryExchangeRate <= 0)
            throw new DomainException("INVALID_EXCHANGE_RATE", "La cotización primaria debe ser mayor a cero.");

        // 10. SecondaryExchangeRate > 0
        if (request.SecondaryExchangeRate <= 0)
            throw new DomainException("INVALID_SECONDARY_EXCHANGE_RATE", "La cotización secundaria debe ser mayor a cero.");

        // 11. Payments not empty
        if (request.Payments.Count == 0)
            throw new DomainException("NO_PAYMENTS", "Debe informar al menos una forma de pago.");

        // Get member record for CashBox permission checks
        var currentMember = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new ForbiddenException("No sos miembro de esta familia.");

        // 12-13. Validate each payment
        var currencyCode = request.CurrencyCode.ToUpperInvariant();
        var paymentEntities = new List<MovementPayment>();

        foreach (var p in request.Payments)
        {
            if (p.Amount <= 0)
                throw new DomainException("INVALID_PAYMENT_AMOUNT", "El importe de cada forma de pago debe ser mayor a cero.");

            if (!Enum.TryParse<PaymentMethodType>(p.PaymentMethodType, true, out var pmType))
                throw new DomainException("INVALID_PAYMENT_METHOD", $"Tipo de pago inválido: '{p.PaymentMethodType}'.");

            var payment = new MovementPayment
            {
                FamilyId = familyId,
                MovementPaymentId = Guid.NewGuid(),
                PaymentMethodType = pmType,
                Amount = p.Amount,
            };

            switch (pmType)
            {
                case PaymentMethodType.CashBox:
                    if (!p.CashBoxId.HasValue)
                        throw new DomainException("MISSING_CASH_BOX", "Debe seleccionar una caja.");
                    var cashBox = await _db.CashBoxes
                        .FirstOrDefaultAsync(cb => cb.FamilyId == familyId && cb.CashBoxId == p.CashBoxId.Value, cancellationToken)
                        ?? throw new DomainException("INVALID_CASH_BOX", "La caja no existe.");
                    if (!cashBox.IsActive)
                        throw new DomainException("INACTIVE_CASH_BOX", "La caja está inactiva.");
                    if (cashBox.CurrencyCode != currencyCode)
                        throw new DomainException("CURRENCY_MISMATCH", $"La caja '{cashBox.Name}' tiene moneda {cashBox.CurrencyCode}, pero el movimiento es en {currencyCode}.");
                    var hasPermission = await _db.CashBoxPermissions
                        .AnyAsync(cp => cp.FamilyId == familyId && cp.CashBoxId == p.CashBoxId.Value && cp.MemberId == currentMember.MemberId, cancellationToken);
                    if (!hasPermission)
                        throw new ForbiddenException($"No tenés permiso para operar la caja '{cashBox.Name}'.");
                    payment.CashBoxId = p.CashBoxId;
                    break;

                case PaymentMethodType.BankAccount:
                    if (!p.BankAccountId.HasValue)
                        throw new DomainException("MISSING_BANK_ACCOUNT", "Debe seleccionar un banco.");
                    var bank = await _db.BankAccounts
                        .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == p.BankAccountId.Value, cancellationToken)
                        ?? throw new DomainException("INVALID_BANK_ACCOUNT", "El banco no existe.");
                    if (!bank.IsActive)
                        throw new DomainException("INACTIVE_BANK_ACCOUNT", "El banco está inactivo.");
                    if (bank.CurrencyCode != currencyCode)
                        throw new DomainException("CURRENCY_MISMATCH", $"El banco '{bank.Name}' tiene moneda {bank.CurrencyCode}, pero el movimiento es en {currencyCode}.");
                    payment.BankAccountId = p.BankAccountId;
                    break;

                case PaymentMethodType.CreditCard:
                    if (!p.CreditCardId.HasValue)
                        throw new DomainException("MISSING_CREDIT_CARD", "Debe seleccionar una tarjeta.");
                    var card = await _db.CreditCards
                        .Include(c => c.Members)
                        .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.CreditCardId == p.CreditCardId.Value, cancellationToken)
                        ?? throw new DomainException("INVALID_CREDIT_CARD", "La tarjeta no existe.");
                    if (!card.IsActive)
                        throw new DomainException("INACTIVE_CREDIT_CARD", "La tarjeta está inactiva.");
                    if (card.CurrencyCode != currencyCode)
                        throw new DomainException("CURRENCY_MISMATCH", $"La tarjeta '{card.Name}' tiene moneda {card.CurrencyCode}, pero el movimiento es en {currencyCode}.");
                    if (p.Installments is null or < 1 or > 48)
                        throw new DomainException("INVALID_INSTALLMENTS", "Las cuotas deben ser entre 1 y 48.");
                    if (!p.CreditCardMemberId.HasValue)
                        throw new DomainException("MISSING_CARD_MEMBER", "El miembro es requerido para pago con tarjeta.");
                    var member = card.Members.FirstOrDefault(m => m.CreditCardMemberId == p.CreditCardMemberId.Value);
                    if (member is null)
                        throw new DomainException("INVALID_CARD_MEMBER", "El miembro de tarjeta no pertenece a la tarjeta seleccionada.");
                    payment.CreditCardId = p.CreditCardId;
                    payment.CreditCardMemberId = p.CreditCardMemberId;
                    payment.Installments = p.Installments;

                    // Calculate bonification
                    if (!string.IsNullOrWhiteSpace(p.BonificationType))
                    {
                        if (!Enum.TryParse<BonificationType>(p.BonificationType, true, out var bonifType))
                            throw new DomainException("INVALID_BONIFICATION_TYPE", "Tipo de bonificación inválido. Valores: Percentage, FixedAmount.");

                        if (p.BonificationValue is null or <= 0)
                            throw new DomainException("INVALID_BONIFICATION_VALUE", "El valor de bonificación debe ser mayor a cero.");

                        if (bonifType == BonificationType.Percentage && p.BonificationValue > 100)
                            throw new DomainException("INVALID_BONIFICATION_PERCENTAGE", "El porcentaje de bonificación no puede superar 100.");

                        payment.BonificationType = bonifType;
                        payment.BonificationValue = p.BonificationValue;
                        payment.BonificationAmount = bonifType == BonificationType.Percentage
                            ? Math.Round(p.Amount * p.BonificationValue.Value / 100, 2)
                            : p.BonificationValue.Value;

                        if (payment.BonificationAmount > p.Amount)
                            throw new DomainException("BONIFICATION_EXCEEDS_AMOUNT", "La bonificación no puede superar el importe del pago.");

                        payment.NetAmount = p.Amount - payment.BonificationAmount.Value;
                    }
                    else
                    {
                        payment.NetAmount = p.Amount;
                    }
                    break;
            }

            paymentEntities.Add(payment);
        }

        // 14. Sum of payments == amount
        var paymentSum = request.Payments.Sum(p => p.Amount);
        if (paymentSum != request.Amount)
            throw new DomainException("PAYMENT_SUM_MISMATCH", $"La suma de pagos ({paymentSum}) no coincide con el importe del movimiento ({request.Amount}).");

        // 15-16. Calculate amounts
        var amountInPrimary = request.Amount * request.PrimaryExchangeRate;
        var amountInSecondary = request.Amount * request.SecondaryExchangeRate;

        // Create movement
        var movement = new Movement
        {
            FamilyId = familyId,
            MovementId = Guid.NewGuid(),
            Date = request.Date,
            MovementType = movementType,
            Amount = request.Amount,
            CurrencyCode = currencyCode,
            PrimaryExchangeRate = request.PrimaryExchangeRate,
            SecondaryExchangeRate = request.SecondaryExchangeRate,
            AmountInPrimary = amountInPrimary,
            AmountInSecondary = amountInSecondary,
            Description = request.Description?.Trim(),
            SubcategoryId = request.SubcategoryId,
            AccountingType = request.AccountingType,
            IsOrdinary = request.IsOrdinary,
            CostCenterId = request.CostCenterId,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Web" : request.Source,
        };

        await _db.Movements.AddAsync(movement, cancellationToken);

        // Add payments and apply side effects
        foreach (var payment in paymentEntities)
        {
            payment.MovementId = movement.MovementId;
            await _db.MovementPayments.AddAsync(payment, cancellationToken);

            // Side effects: update balances
            var sign = movementType == MovementType.Income ? 1 : -1;

            switch (payment.PaymentMethodType)
            {
                case PaymentMethodType.CashBox:
                    var cb = await _db.CashBoxes.FirstAsync(
                        x => x.FamilyId == familyId && x.CashBoxId == payment.CashBoxId!.Value, cancellationToken);
                    cb.Balance += sign * payment.Amount;
                    break;

                case PaymentMethodType.BankAccount:
                    var ba = await _db.BankAccounts.FirstAsync(
                        x => x.FamilyId == familyId && x.BankAccountId == payment.BankAccountId!.Value, cancellationToken);
                    ba.Balance += sign * payment.Amount;
                    break;

                case PaymentMethodType.CreditCard:
                    // Generate installments
                    var installments = GenerateInstallments(
                        familyId, payment, request.Date);
                    foreach (var inst in installments)
                        await _db.CreditCardInstallments.AddAsync(inst, cancellationToken);
                    break;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(movement, paymentEntities);
    }

    /// <summary>
    /// Generates installment records for a credit card payment.
    /// Bonification is distributed across installments in order:
    /// each installment absorbs as much bonification as possible (up to its projected amount),
    /// and any remainder carries over to the next installment.
    /// </summary>
    private static List<CreditCardInstallment> GenerateInstallments(
        Guid familyId, MovementPayment payment, DateOnly movementDate)
    {
        var count = payment.Installments!.Value;
        var grossAmount = payment.Amount;
        var totalBonification = payment.BonificationAmount ?? 0m;

        // Divide gross amount evenly, last installment absorbs rounding difference
        var baseAmount = Math.Round(grossAmount / count, 2);
        var remainingBonification = totalBonification;

        var installments = new List<CreditCardInstallment>(count);

        for (int i = 1; i <= count; i++)
        {
            var projected = (i == count)
                ? grossAmount - baseAmount * (count - 1)
                : baseAmount;

            // Apply bonification in order: this installment absorbs up to its full projected amount
            var bonifApplied = Math.Min(remainingBonification, projected);
            remainingBonification -= bonifApplied;

            var effective = projected - bonifApplied;

            installments.Add(new CreditCardInstallment
            {
                FamilyId = familyId,
                CreditCardInstallmentId = Guid.NewGuid(),
                MovementPaymentId = payment.MovementPaymentId,
                InstallmentNumber = i,
                ProjectedAmount = projected,
                BonificationApplied = bonifApplied,
                EffectiveAmount = effective,
                EstimatedDate = movementDate.AddMonths(i - 1),
            });
        }

        return installments;
    }

    private static MovementDto MapToDto(Movement m, List<MovementPayment> payments)
    {
        return new MovementDto
        {
            MovementId = m.MovementId,
            Date = m.Date,
            MovementType = m.MovementType.ToString(),
            Amount = m.Amount,
            CurrencyCode = m.CurrencyCode,
            PrimaryExchangeRate = m.PrimaryExchangeRate,
            SecondaryExchangeRate = m.SecondaryExchangeRate,
            AmountInPrimary = m.AmountInPrimary,
            AmountInSecondary = m.AmountInSecondary,
            Description = m.Description,
            SubcategoryId = m.SubcategoryId,
            AccountingType = m.AccountingType,
            IsOrdinary = m.IsOrdinary,
            CostCenterId = m.CostCenterId,
            Source = m.Source,
            Payments = payments.Select(p => new MovementPaymentDto
            {
                MovementPaymentId = p.MovementPaymentId,
                PaymentMethodType = p.PaymentMethodType.ToString(),
                Amount = p.Amount,
                CashBoxId = p.CashBoxId,
                BankAccountId = p.BankAccountId,
                CreditCardId = p.CreditCardId,
                CreditCardMemberId = p.CreditCardMemberId,
                Installments = p.Installments,
                BonificationType = p.BonificationType?.ToString(),
                BonificationValue = p.BonificationValue,
                BonificationAmount = p.BonificationAmount,
                NetAmount = p.NetAmount,
            }).ToList(),
        };
    }
}
