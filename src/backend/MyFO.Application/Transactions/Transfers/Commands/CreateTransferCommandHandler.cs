using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.Transfers.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions;
using MyFO.Domain.Transactions.Enums;
namespace MyFO.Application.Transactions.Transfers.Commands;

public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, TransferDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateTransferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TransferDto> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate family
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

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

        // 15. Calculate amounts in primary/secondary
        var amountInPrimary = request.Amount * request.FromPrimaryExchangeRate;
        var amountInSecondary = request.Amount * request.FromSecondaryExchangeRate;
        var amountToInPrimary = request.AmountTo * request.ToPrimaryExchangeRate;
        var amountToInSecondary = request.AmountTo * request.ToSecondaryExchangeRate;

        // ExchangeRate = origin→destination TC (metadata for cashflow reports)
        var exchangeRate = request.Amount != 0 ? request.AmountTo / request.Amount : 1m;

        // Determine if current user can operate the destination (auto-confirm or pending)
        var canOperateDest = await CanOperateDestinationAsync(familyId, request.ToCashBoxId, request.ToBankAccountId, cancellationToken);
        var status = canOperateDest ? TransferStatus.Confirmed : TransferStatus.PendingConfirmation;

        // Create transfer
        var transfer = new Transfer
        {
            FamilyId = familyId,
            TransferId = Guid.NewGuid(),
            Date = request.Date,
            FromCashBoxId = request.FromCashBoxId,
            FromBankAccountId = request.FromBankAccountId,
            ToCashBoxId = request.ToCashBoxId,
            ToBankAccountId = request.ToBankAccountId,
            Amount = request.Amount,
            ExchangeRate = exchangeRate,
            FromPrimaryExchangeRate = request.FromPrimaryExchangeRate,
            FromSecondaryExchangeRate = request.FromSecondaryExchangeRate,
            ToPrimaryExchangeRate = request.ToPrimaryExchangeRate,
            ToSecondaryExchangeRate = request.ToSecondaryExchangeRate,
            AmountTo = request.AmountTo,
            AmountToInPrimary = amountToInPrimary,
            AmountToInSecondary = amountToInSecondary,
            AmountInPrimary = amountInPrimary,
            AmountInSecondary = amountInSecondary,
            Description = request.Description?.Trim(),
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Web" : request.Source,
            Status = status,
            IsAutoConfirmed = canOperateDest,
        };

        await _db.Transfers.AddAsync(transfer, cancellationToken);

        // Side effects: update balances only if confirmed
        if (status != TransferStatus.Confirmed)
        {
            await _db.SaveChangesAsync(cancellationToken);

            // Load navigation properties for response (even without balance changes)
            string? fromCashBoxNameEarly = null, fromBankAccountNameEarly = null, toCashBoxNameEarly = null, toBankAccountNameEarly = null;
            if (transfer.FromCashBoxId.HasValue)
            {
                var cb = await _db.CashBoxes.FirstOrDefaultAsync(x => x.FamilyId == familyId && x.CashBoxId == transfer.FromCashBoxId.Value, cancellationToken);
                fromCashBoxNameEarly = cb?.Name;
                fromCurrencyCode = cb?.CurrencyCode ?? fromCurrencyCode;
            }
            if (transfer.FromBankAccountId.HasValue)
            {
                var ba = await _db.BankAccounts.FirstOrDefaultAsync(x => x.FamilyId == familyId && x.BankAccountId == transfer.FromBankAccountId.Value, cancellationToken);
                fromBankAccountNameEarly = ba?.Name;
                fromCurrencyCode = ba?.CurrencyCode ?? fromCurrencyCode;
            }
            if (transfer.ToCashBoxId.HasValue)
            {
                var cb = await _db.CashBoxes.FirstOrDefaultAsync(x => x.FamilyId == familyId && x.CashBoxId == transfer.ToCashBoxId.Value, cancellationToken);
                toCashBoxNameEarly = cb?.Name;
                toCurrencyCode = cb?.CurrencyCode ?? toCurrencyCode;
            }
            if (transfer.ToBankAccountId.HasValue)
            {
                var ba = await _db.BankAccounts.FirstOrDefaultAsync(x => x.FamilyId == familyId && x.BankAccountId == transfer.ToBankAccountId.Value, cancellationToken);
                toBankAccountNameEarly = ba?.Name;
                toCurrencyCode = ba?.CurrencyCode ?? toCurrencyCode;
            }
            return new TransferDto
            {
                TransferId = transfer.TransferId,
                Date = transfer.Date,
                FromCashBoxId = transfer.FromCashBoxId,
                FromCashBoxName = fromCashBoxNameEarly,
                FromBankAccountId = transfer.FromBankAccountId,
                FromBankAccountName = fromBankAccountNameEarly,
                ToCashBoxId = transfer.ToCashBoxId,
                ToCashBoxName = toCashBoxNameEarly,
                ToBankAccountId = transfer.ToBankAccountId,
                ToBankAccountName = toBankAccountNameEarly,
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
                Source = transfer.Source,
                RowVersion = transfer.RowVersion,
                Status = transfer.Status.ToString(),
                IsAutoConfirmed = transfer.IsAutoConfirmed,
                CreatorUserId = transfer.CreatedBy.ToString(),
            };
        }

        if (hasFromCashBox)
        {
            var fromCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == request.FromCashBoxId!.Value, cancellationToken);
            fromCb.Balance -= request.Amount;
        }
        else
        {
            var fromBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == request.FromBankAccountId!.Value, cancellationToken);
            fromBa.Balance -= request.Amount;
        }

        if (hasToCashBox)
        {
            var toCb = await _db.CashBoxes.FirstAsync(
                x => x.FamilyId == familyId && x.CashBoxId == request.ToCashBoxId!.Value, cancellationToken);
            toCb.Balance += request.AmountTo;
        }
        else
        {
            var toBa = await _db.BankAccounts.FirstAsync(
                x => x.FamilyId == familyId && x.BankAccountId == request.ToBankAccountId!.Value, cancellationToken);
            toBa.Balance += request.AmountTo;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Load navigation properties for response
        string? fromCashBoxName = null, fromBankAccountName = null, toCashBoxName = null, toBankAccountName = null;

        if (transfer.FromCashBoxId.HasValue)
        {
            var cb = await _db.CashBoxes.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.FromCashBoxId.Value, cancellationToken);
            fromCashBoxName = cb?.Name;
            fromCurrencyCode = cb?.CurrencyCode ?? fromCurrencyCode;
        }
        if (transfer.FromBankAccountId.HasValue)
        {
            var ba = await _db.BankAccounts.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.FromBankAccountId.Value, cancellationToken);
            fromBankAccountName = ba?.Name;
            fromCurrencyCode = ba?.CurrencyCode ?? fromCurrencyCode;
        }
        if (transfer.ToCashBoxId.HasValue)
        {
            var cb = await _db.CashBoxes.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.CashBoxId == transfer.ToCashBoxId.Value, cancellationToken);
            toCashBoxName = cb?.Name;
            toCurrencyCode = cb?.CurrencyCode ?? toCurrencyCode;
        }
        if (transfer.ToBankAccountId.HasValue)
        {
            var ba = await _db.BankAccounts.FirstOrDefaultAsync(
                x => x.FamilyId == familyId && x.BankAccountId == transfer.ToBankAccountId.Value, cancellationToken);
            toBankAccountName = ba?.Name;
            toCurrencyCode = ba?.CurrencyCode ?? toCurrencyCode;
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
            Source = transfer.Source,
            Status = transfer.Status.ToString(),
            IsAutoConfirmed = transfer.IsAutoConfirmed,
            CreatorUserId = transfer.CreatedBy.ToString(),
        };
    }

    private async Task<bool> CanOperateDestinationAsync(Guid familyId, Guid? toCashBoxId, Guid? toBankAccountId, CancellationToken cancellationToken)
    {
        // BankAccount destination: always can operate
        if (toBankAccountId.HasValue)
            return true;

        // CashBox destination: mirrors GetCashBoxesQueryHandler CanOperate logic:
        //   - Explicit Operate → true
        //   - Explicit View    → false
        //   - No explicit entry + admin → true
        //   - No explicit entry + non-admin → false
        if (toCashBoxId.HasValue)
        {
            var member = await _db.FamilyMembers
                .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId, cancellationToken);

            if (member == null)
                return false;

            var permission = await _db.CashBoxPermissions
                .FirstOrDefaultAsync(p => p.CashBoxId == toCashBoxId.Value && p.MemberId == member.MemberId, cancellationToken);

            // Record exists (non-deleted) = has permission to operate
            return permission != null;
        }

        return false;
    }

}
