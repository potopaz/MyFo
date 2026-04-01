using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.BankAccounts.DTOs;

namespace MyFO.Application.Transactions.BankAccounts.Queries;

public record GetBankAccountsQuery : IRequest<List<BankAccountDto>>;
