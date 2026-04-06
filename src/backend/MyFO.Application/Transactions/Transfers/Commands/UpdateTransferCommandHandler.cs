using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.Transfers.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Transfers.Commands;

public class UpdateTransferCommandHandler : IRequestHandler<UpdateTransferCommand, TransferDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateTransferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TransferDto> Handle(UpdateTransferCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate family
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Load existing transfer
        var transfer = await _db.Transfers
            .FirstOrDefaultAsync(t => t.FamilyId == familyId && t.TransferId == request.TransferId, cancellationToken)
            ?? throw new NotFoundException("Transfer", request.TransferId);

        if (request.ClientRowVersion.HasValue && request.ClientRowVersion.Value != transfer.RowVersion)
            throw new ConflictException("CONCURRENT_MODIFICATION",
                "La transferencia fue modificada por otro usuario. Recargá el formulario para ver los cambios actuales.");

        if (transfer.IsReconciled)
            throw new DomainException("TRANSFER_RECONCILED",
                "No se puede modificar un traspaso conciliado.");

        // Only PendingConfirmation transfers can be edited, and only by the creator
        if (transfer.Status != TransferStatus.PendingConfirmation)
            throw new DomainException("INVALID_STATUS", "Solo se pueden editar transferencias en estado pendiente de confirmación.");

        if (transfer.CreatedBy != _currentUser.UserId)
            throw new ForbiddenException("Solo el creador puede editar una transferencia pendiente.");

        // --- Reverse old balances ---
        if (transfer.FromCashBoxId.HasValue)
        {
            var oldFromCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.FromCashBoxId.Value, cancellationToken);
            oldFromCb.Balance += transfer.Amount;
        }
        else if (transfer.FromBankAccountId.HasValue)
        {
            var oldFromBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.FromBankAccountId.Value, cancellationToken);
            oldFromBa.Balance += transfer.Amount;
        }

        if (transfer.ToCashBoxId.HasValue)
        {
            var oldToCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.ToCashBoxId.Value, cancellationToken);
            oldToCb.Balance -= transfer.AmountTo;
        }
        else if (transfer.ToBankAccountId.HasValue)
        {
            var oldToBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.ToBankAccountId.Value, cancellationToken);
            oldToBa.Balance -= transfer.AmountTo;
        }

        // --- Validate new data ---

        // 3. Exactly one of FromCashBoxId/FromBankAccountId must have value
        var hasFromCashBox = request.FromCashBoxId.HasValue;
        var hasFromBankAccount = request.FromBankAccountId.HasValue;
        if (hasFromCashBox == hasFromBankAccount)
            throw new DomainException("INVALID_FROM_SOURCE", "Debe especificar exactamente una cuenta de origen (caja o banco).");

        // 4. Exactly one of ToCashBoxId/ToBankAccountId must have value
        var hasToCashBox = request.ToCashBoxId.HasValue;
        var hasToBankAccount = request.ToBankAccountId.HasValue;
        if (hasToCashBox == hasToBankAccount)
            throw new DomainException("INVALID_TO_SOURCE", "Debe especificar exactamente una cuenta de destino (caja o banco).");

        // 5. Load from entity, must exist and be active
        string fromCurrencyCode;
        if (hasFromCashBox)
        {
            var fromCashBox = await _db.CashBoxes
                .FirstOrDefaultAsync(cb => cb.FamilyId == familyId && cb.CashBoxId == request.FromCashBoxId!.Value, cancellationToken)
                ?? throw new DomainException("INVALID_FROM_CASH_BOX", "La caja de origen no existe.");
            if (!fromCashBox.IsActive)
                throw new DomainException("INACTIVE_FROM_CASH_BOX", "La caja de origen está inactiva.");
            fromCurrencyCode = fromCashBox.CurrencyCode;
        }
        else
        {
            var fromBankAccount = await _db.BankAccounts
                .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == request.FromBankAccountId!.Value, cancellationToken)
                ?? throw new DomainException("INVALID_FROM_BANK_ACCOUNT", "El banco de origen no existe.");
            if (!fromBankAccount.IsActive)
                throw new DomainException("INACTIVE_FROM_BANK_ACCOUNT", "El banco de origen está inactivo.");
            fromCurrencyCode = fromBankAccount.CurrencyCode;
        }

        // 6. Load to entity, must exist and be active
        string toCurrencyCode;
        if (hasToCashBox)
        {
            var toCashBox = await _db.CashBoxes
                .FirstOrDefaultAsync(cb => cb.FamilyId == familyId && cb.CashBoxId == request.ToCashBoxId!.Value, cancellationToken)
                ?? throw new DomainException("INVALID_TO_CASH_BOX", "La caja de destino no existe.");
            if (!toCashBox.IsActive)
                throw new DomainException("INACTIVE_TO_CASH_BOX", "La caja de destino está inactiva.");
            toCurrencyCode = toCashBox.CurrencyCode;
        }
        else
        {
            var toBankAccount = await _db.BankAccounts
                .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == request.ToBankAccountId!.Value, cancellationToken)
                ?? throw new DomainException("INVALID_TO_BANK_ACCOUNT", "El banco de destino no existe.");
            if (!toBankAccount.IsActive)
                throw new DomainException("INACTIVE_TO_BANK_ACCOUNT", "El banco de destino está inactivo.");
            toCurrencyCode = toBankAccount.CurrencyCode;
        }

        // 7. From != To (same type + same id)
        if (hasFromCashBox && hasToCashBox && request.FromCashBoxId == request.ToCashBoxId)
            throw new DomainException("SAME_SOURCE_DESTINATION", "La cuenta de origen y destino no pueden ser la misma.");
        if (hasFromBankAccount && hasToBankAccount && request.FromBankAccountId == request.ToBankAccountId)
            throw new DomainException("SAME_SOURCE_DESTINATION", "La cuenta de origen y destino no pueden ser la misma.");

        // 8. Amount > 0
        if (request.Amount <= 0)
            throw new DomainException("INVALID_AMOUNT", "El importe de origen debe ser mayor a cero.");

        // 9. AmountTo > 0
        if (request.AmountTo <= 0)
            throw new DomainException("INVALID_AMOUNT_TO", "El importe de destino debe ser mayor a cero.");

        // 10. If same currency: AmountTo must equal Amount
        if (fromCurrencyCode == toCurrencyCode && request.AmountTo != request.Amount)
            throw new DomainException("AMOUNT_MISMATCH", "Cuando la moneda de origen y destino son iguales, el importe de destino debe ser igual al de origen.");

        // 11. FromPrimaryExchangeRate > 0
        if (request.FromPrimaryExchangeRate <= 0)
            throw new DomainException("INVALID_EXCHANGE_RATE", "La cotización primaria de origen debe ser mayor a cero.");

        // 12. FromSecondaryExchangeRate > 0
        if (request.FromSecondaryExchangeRate <= 0)
            throw new DomainException("INVALID_SECONDARY_EXCHANGE_RATE", "La cotización secundaria de origen debe ser mayor a cero.");

        // 13. ToPrimaryExchangeRate > 0
        if (request.ToPrimaryExchangeRate <= 0)
            throw new DomainException("INVALID_TO_PRIMARY_EXCHANGE_RATE", "La cotización primaria de destino debe ser mayor a cero.");

        // 14. ToSecondaryExchangeRate > 0
        if (request.ToSecondaryExchangeRate <= 0)
            throw new DomainException("INVALID_TO_SECONDARY_EXCHANGE_RATE", "La cotización secundaria de destino debe ser mayor a cero.");

        // 15. Calculate amounts
        var amountInPrimary = request.Amount * request.FromPrimaryExchangeRate;
        var amountInSecondary = request.Amount * request.FromSecondaryExchangeRate;
        var amountToInPrimary = request.AmountTo * request.ToPrimaryExchangeRate;
        var amountToInSecondary = request.AmountTo * request.ToSecondaryExchangeRate;

        var exchangeRate = request.Amount != 0 ? request.AmountTo / request.Amount : 1m;

        // Update transfer
        transfer.Date = request.Date;
        transfer.FromCashBoxId = request.FromCashBoxId;
        transfer.FromBankAccountId = request.FromBankAccountId;
        transfer.ToCashBoxId = request.ToCashBoxId;
        transfer.ToBankAccountId = request.ToBankAccountId;
        transfer.Amount = request.Amount;
        transfer.ExchangeRate = exchangeRate;
        transfer.FromPrimaryExchangeRate = request.FromPrimaryExchangeRate;
        transfer.FromSecondaryExchangeRate = request.FromSecondaryExchangeRate;
        transfer.ToPrimaryExchangeRate = request.ToPrimaryExchangeRate;
        transfer.ToSecondaryExchangeRate = request.ToSecondaryExchangeRate;
        transfer.AmountTo = request.AmountTo;
        transfer.AmountToInPrimary = amountToInPrimary;
        transfer.AmountToInSecondary = amountToInSecondary;
        transfer.AmountInPrimary = amountInPrimary;
        transfer.AmountInSecondary = amountInSecondary;
        transfer.Description = request.Description?.Trim();
        transfer.RowVersion++;

        // Apply new side effects
        if (hasFromCashBox)
        {
            var newFromCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == request.FromCashBoxId!.Value, cancellationToken);
            newFromCb.Balance -= request.Amount;
        }
        else
        {
            var newFromBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == request.FromBankAccountId!.Value, cancellationToken);
            newFromBa.Balance -= request.Amount;
        }

        if (hasToCashBox)
        {
            var newToCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == request.ToCashBoxId!.Value, cancellationToken);
            newToCb.Balance += request.AmountTo;
        }
        else
        {
            var newToBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == request.ToBankAccountId!.Value, cancellationToken);
            newToBa.Balance += request.AmountTo;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Load names for response
        string? fromCashBoxName = null, fromBankAccountName = null, toCashBoxName = null, toBankAccountName = null;

        if (transfer.FromCashBoxId.HasValue)
        {
            var cb = await _db.CashBoxes.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.FromCashBoxId.Value, cancellationToken);
            fromCashBoxName = cb?.Name;
        }
        if (transfer.FromBankAccountId.HasValue)
        {
            var ba = await _db.BankAccounts.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.FromBankAccountId.Value, cancellationToken);
            fromBankAccountName = ba?.Name;
        }
        if (transfer.ToCashBoxId.HasValue)
        {
            var cb = await _db.CashBoxes.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.ToCashBoxId.Value, cancellationToken);
            toCashBoxName = cb?.Name;
        }
        if (transfer.ToBankAccountId.HasValue)
        {
            var ba = await _db.BankAccounts.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.ToBankAccountId.Value, cancellationToken);
            toBankAccountName = ba?.Name;
        }

        return new TransferDto
        {
            TransferId = transfer.TransferId,
            Date = transfer.Date,
            FromCashBoxId = transfer.FromCashBoxId,
            FromCashBoxName = fromCashBoxName,
            FromBankAccountId = transfer.FromBankAccountId,
            FromBankAccountName = fromBankAccountName,
            ToCashBoxId = transfer.ToCashBoxId,
            ToCashBoxName = toCashBoxName,
            ToBankAccountId = transfer.ToBankAccountId,
            ToBankAccountName = toBankAccountName,
            FromCurrencyCode = fromCurrencyCode,
            ToCurrencyCode = toCurrencyCode,
            Amount = transfer.Amount,
            ExchangeRate = transfer.ExchangeRate,
            FromPrimaryExchangeRate = transfer.FromPrimaryExchangeRate,
            FromSecondaryExchangeRate = transfer.FromSecondaryExchangeRate,
            ToPrimaryExchangeRate = transfer.ToPrimaryExchangeRate,
            ToSecondaryExchangeRate = transfer.ToSecondaryExchangeRate,
            AmountTo = transfer.AmountTo,
            AmountToInPrimary = transfer.AmountToInPrimary,
            AmountToInSecondary = transfer.AmountToInSecondary,
            AmountInPrimary = transfer.AmountInPrimary,
            AmountInSecondary = transfer.AmountInSecondary,
            Description = transfer.Description,
            RowVersion = transfer.RowVersion,
            Status = transfer.Status.ToString(),
            IsAutoConfirmed = transfer.IsAutoConfirmed,
            CreatorUserId = transfer.CreatedBy.ToString(),
        };
    }

}
