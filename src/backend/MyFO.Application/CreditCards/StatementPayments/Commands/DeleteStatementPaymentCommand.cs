using MyFO.Application.Common.Mediator;

namespace MyFO.Application.CreditCards.StatementPayments.Commands;

public record DeleteStatementPaymentCommand(Guid StatementPaymentId) : IRequest;
