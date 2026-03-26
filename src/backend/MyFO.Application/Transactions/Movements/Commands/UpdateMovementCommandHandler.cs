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

public class UpdateMovementCommandHandler : IRequestHandler<UpdateMovementCommand, MovementDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateMovementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MovementDto> Handle(UpdateMovementCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Load existing movement with payments
        var movement = await _db.Movements
            .Include(m => m.Payments)
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.MovementId == request.MovementId, cancellationToken)
            ?? throw new NotFoundException("Movement", request.MovementId);

        if (request.ClientRowVersion.HasValue && request.ClientRowVersion.Value != movement.RowVersion)
            throw new ConflictException("CONCURRENT_MODIFICATION",
                "El movimiento fue modificado por otro usuario. Recargá el formulario para ver los cambios actuales.");

        var family = await _db.Families
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.FamilyId == familyId && f.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("Family", familyId);

        // --- Identify locked payments early (needed before balance reversal) ---
        var allOldCcPaymentIds = movement.Payments
            .Where(p => p.PaymentMethodType == PaymentMethodType.CreditCard)
            .Select(p => p.MovementPaymentId)
            .ToList();

        var lockedPaymentIds = new HashSet<Guid>();
        if (allOldCcPaymentIds.Count > 0)
        {
            lockedPaymentIds = (await _db.CreditCardInstallments
                .Where(i => allOldCcPaymentIds.Contains(i.MovementPaymentId)
                    && i.StatementPeriodId != null
                    && i.DeletedAt == null)
                .Select(i => i.MovementPaymentId)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        // --- Validate type/currency unchanged when there are locked payments ---
        if (lockedPaymentIds.Count > 0)
        {
            var newCurrencyCode = request.CurrencyCode.ToUpperInvariant();
            if (newCurrencyCode != movement.CurrencyCode)
                throw new DomainException("CURRENCY_LOCKED",
                    "No se puede cambiar la moneda porque hay pagos con cuotas incluidas en un resumen de tarjeta.");

            if (!string.IsNullOrWhiteSpace(request.MovementType)
                && Enum.TryParse<MovementType>(request.MovementType, true, out var requestedType)
                && requestedType != movement.MovementType)
                throw new DomainException("MOVEMENT_TYPE_LOCKED",
                    "No se puede cambiar el tipo porque hay pagos con cuotas incluidas en un resumen de tarjeta.");
        }

        // --- Reverse old balances (skip locked payments — they don't change) ---
        var oldSign = movement.MovementType == MovementType.Income ? 1 : -1;
        foreach (var oldPayment in movement.Payments)
        {
            if (lockedPaymentIds.Contains(oldPayment.MovementPaymentId))
                continue;

            switch (oldPayment.PaymentMethodType)
            {
                case PaymentMethodType.CashBox:
                    var oldCb = await _db.CashBoxes.FirstAsync(
                        x => x.FamilyId == familyId && x.CashBoxId == oldPayment.CashBoxId!.Value, cancellationToken);
                    oldCb.Balance -= oldSign * oldPayment.Amount;
                    break;
                case PaymentMethodType.BankAccount:
                    var oldBa = await _db.BankAccounts.FirstAsync(
                        x => x.FamilyId == familyId && x.BankAccountId == oldPayment.BankAccountId!.Value, cancellationToken);
                    oldBa.Balance -= oldSign * oldPayment.Amount;
                    break;
            }
        }

        // --- Validate new data (same as Create) ---
        if (request.Amount <= 0)
            throw new DomainException("INVALID_AMOUNT", "El importe debe ser mayor a cero.");

        var currencyCode = request.CurrencyCode.ToUpperInvariant();
        var familyCurrency = await _db.FamilyCurrencies
            .FirstOrDefaultAsync(fc => fc.FamilyId == familyId
                && fc.Currency.Code == currencyCode, cancellationToken);
        if (familyCurrency is null)
            throw new DomainException("INVALID_CURRENCY", $"La moneda '{request.CurrencyCode}' no está asociada a la familia.");
        if (!familyCurrency.IsActive)
            throw new DomainException("INACTIVE_CURRENCY", $"La moneda '{request.CurrencyCode}' está inactiva.");

        var subcategory = await _db.Subcategories
            .FirstOrDefaultAsync(s => s.FamilyId == familyId && s.SubcategoryId == request.SubcategoryId, cancellationToken)
            ?? throw new DomainException("INVALID_SUBCATEGORY", "La subcategoría no existe.");
        if (!subcategory.IsActive)
            throw new DomainException("INACTIVE_SUBCATEGORY", "La subcategoría está inactiva.");

        MovementType movementType;
        if (subcategory.SubcategoryType == SubcategoryType.Both)
        {
            if (string.IsNullOrWhiteSpace(request.MovementType))
                throw new DomainException("MOVEMENT_TYPE_REQUIRED", "El tipo de movimiento es requerido para subcategorías de tipo 'Ambos'.");
            if (!Enum.TryParse<MovementType>(request.MovementType, true, out movementType))
                throw new DomainException("INVALID_MOVEMENT_TYPE", "Tipo de movimiento inválido.");
        }
        else
        {
            movementType = subcategory.SubcategoryType == SubcategoryType.Income
                ? MovementType.Income
                : MovementType.Expense;
        }

        if (request.CostCenterId.HasValue)
        {
            var costCenter = await _db.CostCenters
                .FirstOrDefaultAsync(cc => cc.FamilyId == familyId && cc.CostCenterId == request.CostCenterId.Value, cancellationToken)
                ?? throw new DomainException("INVALID_COST_CENTER", "El centro de costo no existe.");
            if (!costCenter.IsActive)
                throw new DomainException("INACTIVE_COST_CENTER", "El centro de costo está inactivo.");
        }

        if (request.PrimaryExchangeRate <= 0)
            throw new DomainException("INVALID_EXCHANGE_RATE", "La cotización primaria debe ser mayor a cero.");
        if (request.SecondaryExchangeRate <= 0)
            throw new DomainException("INVALID_SECONDARY_EXCHANGE_RATE", "La cotización secundaria debe ser mayor a cero.");

        if (request.Payments.Count == 0)
            throw new DomainException("NO_PAYMENTS", "Debe informar al menos una forma de pago.");

        // Get member record for CashBox permission checks
        var currentMember = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new ForbiddenException("No sos miembro de esta familia.");

        // Validate new payments
        var newPayments = new List<MovementPayment>();
        foreach (var p in request.Payments)
        {
            if (p.Amount <= 0)
                throw new DomainException("INVALID_PAYMENT_AMOUNT", "El importe de cada forma de pago debe ser mayor a cero.");

            if (!Enum.TryParse<PaymentMethodType>(p.PaymentMethodType, true, out var pmType))
                throw new DomainException("INVALID_PAYMENT_METHOD", $"Tipo de pago inválido: '{p.PaymentMethodType}'.");

            var payment = new MovementPayment
            {
                FamilyId = familyId,
                MovementPaymentId = p.MovementPaymentId ?? Guid.NewGuid(),
                MovementId = movement.MovementId,
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
                    var card = await _db.CreditCards.Include(c => c.Members)
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

            newPayments.Add(payment);
        }

        var paymentSum = request.Payments.Sum(p => p.Amount);
        if (paymentSum != request.Amount)
            throw new DomainException("PAYMENT_SUM_MISMATCH", $"La suma de pagos ({paymentSum}) no coincide con el importe del movimiento ({request.Amount}).");

        // Validate locked payments are preserved unchanged in the request
        foreach (var lockedId in lockedPaymentIds)
        {
            var oldPayment = movement.Payments.First(p => p.MovementPaymentId == lockedId);
            var matchingNew = request.Payments.FirstOrDefault(p => p.MovementPaymentId == lockedId);

            if (matchingNew is null)
                throw new DomainException("LOCKED_PAYMENT_REMOVED",
                    "No se puede eliminar un pago con cuotas incluidas en un resumen de tarjeta.");

            if (matchingNew.Amount != oldPayment.Amount
                || matchingNew.CreditCardId != oldPayment.CreditCardId
                || matchingNew.CreditCardMemberId != oldPayment.CreditCardMemberId
                || matchingNew.Installments != oldPayment.Installments)
                throw new DomainException("LOCKED_PAYMENT_MODIFIED",
                    "No se puede modificar un pago con cuotas incluidas en un resumen de tarjeta.");
        }

        // --- Remove old installments for UNLOCKED payments only ---
        var unlockedCcPaymentIds = allOldCcPaymentIds.Where(id => !lockedPaymentIds.Contains(id)).ToList();
        if (unlockedCcPaymentIds.Count > 0)
        {
            var oldInstallments = await _db.CreditCardInstallments
                .Where(i => unlockedCcPaymentIds.Contains(i.MovementPaymentId) && i.DeletedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var inst in oldInstallments)
                _db.CreditCardInstallments.Remove(inst);
        }

        // --- Remove old UNLOCKED payments, keep locked ones, add new ---
        foreach (var old in movement.Payments.ToList())
        {
            if (!lockedPaymentIds.Contains(old.MovementPaymentId))
                _db.MovementPayments.Remove(old);
        }

        // Add only new payments (skip locked ones which are preserved)
        foreach (var np in newPayments)
        {
            if (!lockedPaymentIds.Contains(np.MovementPaymentId))
                await _db.MovementPayments.AddAsync(np, cancellationToken);
        }

        // --- Update movement ---
        movement.Date = request.Date;
        movement.MovementType = movementType;
        movement.Amount = request.Amount;
        movement.CurrencyCode = currencyCode;
        movement.PrimaryExchangeRate = request.PrimaryExchangeRate;
        movement.SecondaryExchangeRate = request.SecondaryExchangeRate;
        movement.AmountInPrimary = request.Amount * request.PrimaryExchangeRate;
        movement.AmountInSecondary = request.Amount * request.SecondaryExchangeRate;
        movement.Description = request.Description?.Trim();
        movement.SubcategoryId = request.SubcategoryId;
        movement.AccountingType = request.AccountingType;
        movement.IsOrdinary = request.IsOrdinary;
        movement.CostCenterId = request.CostCenterId;
        movement.RowVersion++;

        // --- Apply new balances (skip locked payments — unchanged) ---
        var newSign = movementType == MovementType.Income ? 1 : -1;
        foreach (var np in newPayments)
        {
            if (lockedPaymentIds.Contains(np.MovementPaymentId))
                continue;

            switch (np.PaymentMethodType)
            {
                case PaymentMethodType.CashBox:
                    var cb = await _db.CashBoxes.FirstAsync(
                        x => x.FamilyId == familyId && x.CashBoxId == np.CashBoxId!.Value, cancellationToken);
                    cb.Balance += newSign * np.Amount;
                    break;
                case PaymentMethodType.BankAccount:
                    var ba = await _db.BankAccounts.FirstAsync(
                        x => x.FamilyId == familyId && x.BankAccountId == np.BankAccountId!.Value, cancellationToken);
                    ba.Balance += newSign * np.Amount;
                    break;
                case PaymentMethodType.CreditCard:
                    var newInstallments = GenerateInstallments(familyId, np, request.Date);
                    foreach (var inst in newInstallments)
                        await _db.CreditCardInstallments.AddAsync(inst, cancellationToken);
                    break;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new MovementDto
        {
            MovementId = movement.MovementId,
            Date = movement.Date,
            MovementType = movement.MovementType.ToString(),
            Amount = movement.Amount,
            CurrencyCode = movement.CurrencyCode,
            PrimaryExchangeRate = movement.PrimaryExchangeRate,
            SecondaryExchangeRate = movement.SecondaryExchangeRate,
            AmountInPrimary = movement.AmountInPrimary,
            AmountInSecondary = movement.AmountInSecondary,
            Description = movement.Description,
            SubcategoryId = movement.SubcategoryId,
            AccountingType = movement.AccountingType,
            IsOrdinary = movement.IsOrdinary,
            CostCenterId = movement.CostCenterId,
            Source = movement.Source,
            RowVersion = movement.RowVersion,
            Payments = newPayments.Select(p => new MovementPaymentDto
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

    private static List<CreditCardInstallment> GenerateInstallments(
        Guid familyId, MovementPayment payment, DateOnly movementDate)
    {
        var count = payment.Installments!.Value;
        var grossAmount = payment.Amount;
        var totalBonification = payment.BonificationAmount ?? 0m;

        var baseAmount = Math.Round(grossAmount / count, 2);
        var remainingBonification = totalBonification;

        var installments = new List<CreditCardInstallment>(count);

        for (int i = 1; i <= count; i++)
        {
            var projected = (i == count)
                ? grossAmount - baseAmount * (count - 1)
                : baseAmount;

            var bonifApplied = Math.Min(remainingBonification, projected);
            remainingBonification -= bonifApplied;

            installments.Add(new CreditCardInstallment
            {
                FamilyId = familyId,
                CreditCardInstallmentId = Guid.NewGuid(),
                MovementPaymentId = payment.MovementPaymentId,
                InstallmentNumber = i,
                ProjectedAmount = projected,
                BonificationApplied = bonifApplied,
                EffectiveAmount = projected - bonifApplied,
                EstimatedDate = movementDate.AddMonths(i - 1),
            });
        }

        return installments;
    }
}
