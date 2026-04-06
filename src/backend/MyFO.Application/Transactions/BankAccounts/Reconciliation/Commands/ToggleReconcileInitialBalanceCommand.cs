using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public record ToggleReconcileInitialBalanceCommand(
    Guid BankAccountId,
    bool IsReconciled
) : IRequest<bool>;
