using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.BankAccounts.DTOs;

namespace MyFO.Application.Transactions.BankAccounts.Queries;

public class GetBankAccountsQueryHandler : IRequestHandler<GetBankAccountsQuery, List<BankAccountDto>>
{
    private readonly IApplicationDbContext _db;

    public GetBankAccountsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<BankAccountDto>> Handle(GetBankAccountsQuery request, CancellationToken cancellationToken)
    {
        return await _db.BankAccounts
            .OrderBy(b => b.Name)
            .Select(b => new BankAccountDto
            {
                BankAccountId = b.BankAccountId,
                Name = b.Name,
                CurrencyCode = b.CurrencyCode,
                InitialBalance = b.InitialBalance,
                Balance = b.Balance,
                AccountNumber = b.AccountNumber,
                Cbu = b.Cbu,
                Alias = b.Alias,
                IsActive = b.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
