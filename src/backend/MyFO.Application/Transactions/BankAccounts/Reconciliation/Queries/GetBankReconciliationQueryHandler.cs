using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.BankAccounts.Reconciliation.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Queries;

public class GetBankReconciliationQueryHandler : IRequestHandler<GetBankReconciliationQuery, BankReconciliationDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBankReconciliationQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BankReconciliationDto> Handle(GetBankReconciliationQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var bankAccount = await _db.BankAccounts
            .FirstOrDefaultAsync(ba => ba.FamilyId == familyId && ba.BankAccountId == request.BankAccountId, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.BankAccountId);

        var from = request.From;
        var to = request.To;

        // === Load movement payments for this bank ===
        var movementPayments = await _db.MovementPayments
            .Include(mp => mp.Movement)
            .Where(mp => mp.FamilyId == familyId && mp.BankAccountId == request.BankAccountId)
            .ToListAsync(cancellationToken);

        // Load subcategory names for descriptions
        var subcategoryIds = movementPayments.Select(mp => mp.Movement.SubcategoryId).Distinct().ToList();
        var subcategoryNames = await _db.Subcategories
            .Where(sc => sc.FamilyId == familyId && subcategoryIds.Contains(sc.SubcategoryId))
            .Select(sc => new { sc.SubcategoryId, sc.Name })
            .ToDictionaryAsync(sc => sc.SubcategoryId, sc => sc.Name, cancellationToken);

        // === Load transfers involving this bank ===
        var transfers = await _db.Transfers
            .Where(t => t.FamilyId == familyId
                && (t.FromBankAccountId == request.BankAccountId || t.ToBankAccountId == request.BankAccountId))
            .ToListAsync(cancellationToken);

        // Load account names for transfer descriptions
        var otherBankIds = transfers
            .SelectMany(t => new[] { t.FromBankAccountId, t.ToBankAccountId })
            .Where(id => id.HasValue && id.Value != request.BankAccountId)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var otherBankNames = await _db.BankAccounts
            .Where(ba => ba.FamilyId == familyId && otherBankIds.Contains(ba.BankAccountId))
            .Select(ba => new { ba.BankAccountId, ba.Name })
            .ToDictionaryAsync(ba => ba.BankAccountId, ba => ba.Name, cancellationToken);

        var otherCashBoxIds = transfers
            .SelectMany(t => new[] { t.FromCashBoxId, t.ToCashBoxId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var otherCashBoxNames = await _db.CashBoxes
            .Where(cb => cb.FamilyId == familyId && otherCashBoxIds.Contains(cb.CashBoxId))
            .Select(cb => new { cb.CashBoxId, cb.Name })
            .ToDictionaryAsync(cb => cb.CashBoxId, cb => cb.Name, cancellationToken);

        // === Load CC payments for this bank ===
        var ccPayments = await _db.CreditCardPayments
            .Include(p => p.CreditCard)
            .Where(p => p.FamilyId == familyId && p.BankAccountId == request.BankAccountId)
            .ToListAsync(cancellationToken);

        // === Calculate previous reconciled balance ===
        // Includes the reconciled initial balance (always "before" any date) plus reconciled transactions before 'from'.
        decimal previousReconciledBalance = bankAccount.IsInitialBalanceReconciled ? bankAccount.InitialBalance : 0;

        if (from.HasValue)
        {
            foreach (var mp in movementPayments.Where(mp => mp.IsReconciled && mp.Movement.Date < from.Value))
            {
                var sign = mp.Movement.MovementType == MovementType.Income ? 1 : -1;
                previousReconciledBalance += sign * mp.Amount;
            }

            foreach (var t in transfers.Where(t => t.IsReconciled && t.Date < from.Value))
            {
                if (t.FromBankAccountId == request.BankAccountId)
                    previousReconciledBalance -= t.Amount;
                if (t.ToBankAccountId == request.BankAccountId)
                    previousReconciledBalance += t.AmountTo;
            }

            foreach (var cp in ccPayments.Where(cp => cp.IsReconciled && cp.PaymentDate < from.Value))
                previousReconciledBalance -= cp.Amount;
        }

        // === Build items list ===
        var items = new List<BankReconciliationItemDto>();

        // Initial balance row (always shown first)
        items.Add(new BankReconciliationItemDto
        {
            Type = "InitialBalance",
            Id = bankAccount.BankAccountId,
            Date = null,
            Description = "Saldo inicial de la cuenta bancaria",
            Credit = bankAccount.InitialBalance >= 0 ? bankAccount.InitialBalance : null,
            Debit = bankAccount.InitialBalance < 0 ? Math.Abs(bankAccount.InitialBalance) : null,
            IsReconciled = bankAccount.IsInitialBalanceReconciled,
            IsOutsideDateRange = false,
        });

        // Movement payments
        foreach (var mp in movementPayments)
        {
            var description = mp.Movement.Description
                ?? (subcategoryNames.TryGetValue(mp.Movement.SubcategoryId, out var scName) ? scName : "Movimiento");
            var isIncome = mp.Movement.MovementType == MovementType.Income;
            var inRange = IsInDateRange(mp.Movement.Date, from, to);

            if (!inRange && mp.IsReconciled) continue; // Reconciled items outside range are hidden

            items.Add(new BankReconciliationItemDto
            {
                Type = "MovementPayment",
                Id = mp.MovementPaymentId,
                Date = mp.Movement.Date,
                Description = description,
                Credit = isIncome ? mp.Amount : null,
                Debit = !isIncome ? mp.Amount : null,
                IsReconciled = mp.IsReconciled,
                IsOutsideDateRange = !inRange,
            });
        }

        // Transfers
        foreach (var t in transfers)
        {
            var isInRange = IsInDateRange(t.Date, from, to);
            if (!isInRange && t.IsReconciled) continue;

            var isFromThisBank = t.FromBankAccountId == request.BankAccountId;
            var isToThisBank = t.ToBankAccountId == request.BankAccountId;

            string otherPartyName;
            if (isFromThisBank)
            {
                otherPartyName = t.ToBankAccountId.HasValue && otherBankNames.TryGetValue(t.ToBankAccountId.Value, out var n) ? n
                    : t.ToCashBoxId.HasValue && otherCashBoxNames.TryGetValue(t.ToCashBoxId.Value, out var cn) ? cn
                    : "destino";
            }
            else
            {
                otherPartyName = t.FromBankAccountId.HasValue && otherBankNames.TryGetValue(t.FromBankAccountId.Value, out var n) ? n
                    : t.FromCashBoxId.HasValue && otherCashBoxNames.TryGetValue(t.FromCashBoxId.Value, out var cn) ? cn
                    : "origen";
            }

            var description = t.Description ?? (isFromThisBank
                ? $"Traspaso a {otherPartyName}"
                : $"Traspaso desde {otherPartyName}");

            items.Add(new BankReconciliationItemDto
            {
                Type = "Transfer",
                Id = t.TransferId,
                Date = t.Date,
                Description = description,
                Debit = isFromThisBank ? t.Amount : null,
                Credit = isToThisBank ? t.AmountTo : null,
                IsReconciled = t.IsReconciled,
                IsOutsideDateRange = !isInRange,
            });
        }

        // Credit card payments
        foreach (var cp in ccPayments)
        {
            var isInRange = IsInDateRange(cp.PaymentDate, from, to);
            if (!isInRange && cp.IsReconciled) continue;

            items.Add(new BankReconciliationItemDto
            {
                Type = "CreditCardPayment",
                Id = cp.CreditCardPaymentId,
                Date = cp.PaymentDate,
                Description = cp.Description ?? $"Pago TC: {cp.CreditCard.Name}",
                Debit = cp.Amount,
                Credit = null,
                IsReconciled = cp.IsReconciled,
                IsOutsideDateRange = !isInRange,
            });
        }

        // Sort: InitialBalance first, then by date ascending, unreconciled-outside-range last
        items = items
            .OrderBy(i => i.Type == "InitialBalance" ? 0 : 1)
            .ThenBy(i => i.IsOutsideDateRange ? 1 : 0)
            .ThenBy(i => i.Date ?? DateOnly.MinValue)
            .ToList();

        return new BankReconciliationDto
        {
            BankAccountId = bankAccount.BankAccountId,
            BankAccountName = bankAccount.Name,
            CurrencyCode = bankAccount.CurrencyCode,
            PreviousReconciledBalance = previousReconciledBalance,
            Items = items,
        };
    }

    private static bool IsInDateRange(DateOnly date, DateOnly? from, DateOnly? to)
    {
        if (from.HasValue && date < from.Value) return false;
        if (to.HasValue && date > to.Value) return false;
        return true;
    }
}
