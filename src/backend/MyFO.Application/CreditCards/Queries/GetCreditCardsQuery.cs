using MediatR;
using MyFO.Application.CreditCards.DTOs;

namespace MyFO.Application.CreditCards.Queries;

public record GetCreditCardsQuery : IRequest<List<CreditCardDto>>;
