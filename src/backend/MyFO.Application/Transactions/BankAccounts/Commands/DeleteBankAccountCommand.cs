using MediatR;

namespace MyFO.Application.Transactions.BankAccounts.Commands;

public record DeleteBankAccountCommand(Guid BankAccountId) : IRequest;
