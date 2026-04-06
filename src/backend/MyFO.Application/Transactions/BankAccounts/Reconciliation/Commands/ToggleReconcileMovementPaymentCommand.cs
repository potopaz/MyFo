using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public record ToggleReconcileMovementPaymentCommand(
    Guid BankAccountId,
    Guid MovementPaymentId,
    bool IsReconciled
) : IRequest<bool>;
