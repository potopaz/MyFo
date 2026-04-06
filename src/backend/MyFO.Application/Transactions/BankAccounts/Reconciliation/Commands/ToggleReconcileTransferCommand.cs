using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public record ToggleReconcileTransferCommand(
    Guid BankAccountId,
    Guid TransferId,
    bool IsReconciled
) : IRequest<bool>;
