using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.BankAccounts.Commands;

public record DeleteBankAccountCommand(Guid BankAccountId) : IRequest;
