using MediatR;

namespace MyFO.Application.CreditCards.StatementLineItems.Commands;

public record DeleteStatementLineItemCommand(Guid StatementLineItemId) : IRequest;
