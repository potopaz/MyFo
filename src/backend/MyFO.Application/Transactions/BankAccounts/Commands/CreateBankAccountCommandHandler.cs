using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.BankAccounts.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions;

namespace MyFO.Application.Transactions.BankAccounts.Commands;

public class CreateBankAccountCommandHandler : IRequestHandler<CreateBankAccountCommand, BankAccountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateBankAccountCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BankAccountDto> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Check for existing (including soft-deleted) with same name
        var existing = await _db.BankAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.FamilyId == familyId && b.Name == request.Name, cancellationToken);

        BankAccount bankAccount;

        if (existing is not null)
        {
            if (existing.DeletedAt is null)
                throw new DomainException("DUPLICATE_NAME", $"Ya existe una cuenta bancaria con el nombre '{request.Name}'.");

            // Reactivate soft-deleted record and update data
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.CurrencyCode = request.CurrencyCode.ToUpperInvariant();
            existing.InitialBalance = request.InitialBalance;
            existing.Balance = request.InitialBalance;
            existing.AccountNumber = request.AccountNumber;
            existing.Cbu = request.Cbu;
            existing.Alias = request.Alias;
            existing.IsActive = true;
            bankAccount = existing;
        }
        else
        {
            bankAccount = new BankAccount
            {
                FamilyId = familyId,
                BankAccountId = Guid.NewGuid(),
                Name = request.Name,
                CurrencyCode = request.CurrencyCode.ToUpperInvariant(),
                InitialBalance = request.InitialBalance,
                Balance = request.InitialBalance,
                AccountNumber = request.AccountNumber,
                Cbu = request.Cbu,
                Alias = request.Alias
            };
            await _db.BankAccounts.AddAsync(bankAccount, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new BankAccountDto
        {
            BankAccountId = bankAccount.BankAccountId,
            Name = bankAccount.Name,
            CurrencyCode = bankAccount.CurrencyCode,
            InitialBalance = bankAccount.InitialBalance,
            Balance = bankAccount.Balance,
            AccountNumber = bankAccount.AccountNumber,
            Cbu = bankAccount.Cbu,
            Alias = bankAccount.Alias,
            IsActive = bankAccount.IsActive
        };
    }
}
