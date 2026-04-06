using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.BankAccounts.Reconciliation.DTOs;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Queries;

public record GetBankReconciliationQuery(
    Guid BankAccountId,
    DateOnly? From,
    DateOnly? To
) : IRequest<BankReconciliationDto>;
