using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.BankAccounts.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.BankAccounts.Commands;

public class UpdateBankAccountCommandHandler : IRequestHandler<UpdateBankAccountCommand, BankAccountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateBankAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BankAccountDto> Handle(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.BankAccounts
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.BankAccountId == request.BankAccountId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("BankAccount", request.BankAccountId);

        if (entity.CurrencyCode != request.CurrencyCode)
        {
            var hasMovements = await _db.MovementPayments
                .AnyAsync(mp => mp.BankAccountId == entity.BankAccountId, cancellationToken);
            var hasTransfers = await _db.Transfers
                .AnyAsync(t => t.FromBankAccountId == entity.BankAccountId || t.ToBankAccountId == entity.BankAccountId, cancellationToken);
            var hasCCPayments = await _db.CreditCardPayments
                .AnyAsync(cp => cp.BankAccountId == entity.BankAccountId, cancellationToken);
            var hasFrequentMovements = await _db.FrequentMovements
                .AnyAsync(fm => fm.BankAccountId == entity.BankAccountId, cancellationToken);
            if (hasMovements || hasTransfers || hasCCPayments || hasFrequentMovements)
                throw new DomainException("HAS_OPERATIONS", "No se puede modificar la moneda porque el banco tiene operaciones asociadas.");
        }

        entity.Name = request.Name;
        entity.CurrencyCode = request.CurrencyCode;
        // Adjust running balance by the delta in initial balance
        entity.Balance = entity.Balance - entity.InitialBalance + request.InitialBalance;
        entity.InitialBalance = request.InitialBalance;
        entity.Cbu = request.Cbu;
        entity.Alias = request.Alias;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return new BankAccountDto
        {
            BankAccountId = entity.BankAccountId,
            Name = entity.Name,
            CurrencyCode = entity.CurrencyCode,
            InitialBalance = entity.InitialBalance,
            Balance = entity.Balance,
            AccountNumber = entity.AccountNumber,
            Cbu = entity.Cbu,
            Alias = entity.Alias,
            IsActive = entity.IsActive
        };
    }
}
