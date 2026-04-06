using MyFO.Application.Common.Mediator;

namespace MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;

public record ToggleReconcileCreditCardPaymentCommand(
    Guid BankAccountId,
    Guid CreditCardPaymentId,
    bool IsReconciled
) : IRequest<bool>;
