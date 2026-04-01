using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.FrequentMovements.DTOs;
using MyFO.Domain.Accounting.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.FrequentMovements.Commands;

public class CreateFrequentMovementCommandHandler : IRequestHandler<CreateFrequentMovementCommand, FrequentMovementDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateFrequentMovementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FrequentMovementDto> Handle(CreateFrequentMovementCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("NAME_REQUIRED", "El nombre es requerido.");

        var currencyCode = request.CurrencyCode?.ToUpperInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(currencyCode))
        {
            var familyCurrency = await _db.FamilyCurrencies
                .FirstOrDefaultAsync(fc => fc.FamilyId == familyId
                    && fc.Currency.Code == currencyCode, cancellationToken);
            if (familyCurrency is null)
                throw new DomainException("INVALID_CURRENCY", $"La moneda '{currencyCode}' no está asociada a la familia.");
            if (!familyCurrency.IsActive)
                throw new DomainException("INACTIVE_CURRENCY", $"La moneda '{currencyCode}' está inactiva.");
        }

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

        if (request.FrequencyMonths < 1)
            throw new DomainException("INVALID_FREQUENCY", "La frecuencia debe ser al menos 1 mes.");

        Guid? cashBoxId = null;
        Guid? bankAccountId = null;
        Guid? creditCardId = null;
        Guid? creditCardMemberId = null;
        var pmType = PaymentMethodType.CashBox;

        if (!string.IsNullOrWhiteSpace(request.PaymentMethodType) &&
            Enum.TryParse<PaymentMethodType>(request.PaymentMethodType, true, out var parsedPmType))
        {
            pmType = parsedPmType;
            switch (pmType)
            {
                case PaymentMethodType.CashBox when request.CashBoxId.HasValue:
                    var cashBox = await _db.CashBoxes
                        .FirstOrDefaultAsync(cb => cb.FamilyId == familyId && cb.CashBoxId == request.CashBoxId.Value, cancellationToken)
                        ?? throw new DomainException("INVALID_CASH_BOX", "La caja no existe.");
                    if (!cashBox.IsActive)
                        throw new DomainException("INACTIVE_CASH_BOX", "La caja está inactiva.");
                    cashBoxId = request.CashBoxId;
                    break;

                case PaymentMethodType.BankAccount when request.BankAccountId.HasValue:
                    var bank = await _db.BankAccounts
                        .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == request.BankAccountId.Value, cancellationToken)
                        ?? throw new DomainException("INVALID_BANK_ACCOUNT", "El banco no existe.");
                    if (!bank.IsActive)
                        throw new DomainException("INACTIVE_BANK_ACCOUNT", "El banco está inactivo.");
                    bankAccountId = request.BankAccountId;
                    break;

                case PaymentMethodType.CreditCard when request.CreditCardId.HasValue:
                    var card = await _db.CreditCards
                        .Include(c => c.Members)
                        .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.CreditCardId == request.CreditCardId.Value, cancellationToken)
                        ?? throw new DomainException("INVALID_CREDIT_CARD", "La tarjeta no existe.");
                    if (!card.IsActive)
                        throw new DomainException("INACTIVE_CREDIT_CARD", "La tarjeta está inactiva.");
                    creditCardId = request.CreditCardId;
                    if (request.CreditCardMemberId.HasValue)
                    {
                        var member = card.Members.FirstOrDefault(m => m.CreditCardMemberId == request.CreditCardMemberId.Value);
                        if (member is null)
                            throw new DomainException("INVALID_CARD_MEMBER", "El miembro no pertenece a la tarjeta seleccionada.");
                        creditCardMemberId = request.CreditCardMemberId;
                    }
                    break;
            }
        }

        var now = DateTime.UtcNow;
        var nextDueDate = request.NextDueDate ?? DateOnly.FromDateTime(now).AddMonths(request.FrequencyMonths);

        var entity = new FrequentMovement
        {
            FamilyId = familyId,
            FrequentMovementId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            MovementType = movementType,
            Amount = request.Amount,
            CurrencyCode = currencyCode,
            Description = request.Description?.Trim(),
            SubcategoryId = request.SubcategoryId,
            AccountingType = request.AccountingType,
            IsOrdinary = request.IsOrdinary,
            CostCenterId = request.CostCenterId,
            PaymentMethodType = pmType,
            CashBoxId = cashBoxId,
            BankAccountId = bankAccountId,
            CreditCardId = creditCardId,
            CreditCardMemberId = creditCardMemberId,
            FrequencyMonths = request.FrequencyMonths,
            NextDueDate = nextDueDate,
            IsActive = true,
        };

        await _db.FrequentMovements.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    internal static FrequentMovementDto MapToDto(FrequentMovement e) => new()
    {
        FrequentMovementId = e.FrequentMovementId,
        Name = e.Name,
        MovementType = e.MovementType.ToString(),
        Amount = e.Amount,
        CurrencyCode = e.CurrencyCode,
        Description = e.Description,
        SubcategoryId = e.SubcategoryId,
        AccountingType = e.AccountingType,
        IsOrdinary = e.IsOrdinary,
        CostCenterId = e.CostCenterId,
        PaymentMethodType = e.PaymentMethodType.ToString(),
        CashBoxId = e.CashBoxId,
        BankAccountId = e.BankAccountId,
        CreditCardId = e.CreditCardId,
        CreditCardMemberId = e.CreditCardMemberId,
        FrequencyMonths = e.FrequencyMonths,
        LastAppliedAt = e.LastAppliedAt,
        NextDueDate = e.NextDueDate,
        IsActive = e.IsActive,
        RowVersion = e.RowVersion,
    };
}
