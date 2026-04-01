using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.DTOs;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.Queries;

public class GetCreditCardsQueryHandler : IRequestHandler<GetCreditCardsQuery, List<CreditCardDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCreditCardsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CreditCardDto>> Handle(GetCreditCardsQuery request, CancellationToken cancellationToken)
    {
        // Resolve the current user's MemberId to flag their card member
        var currentMemberId = await _db.FamilyMembers
            .Where(m => m.UserId == _currentUser.UserId)
            .Select(m => (Guid?)m.MemberId)
            .FirstOrDefaultAsync(cancellationToken);

        return await _db.CreditCards
            .Include(c => c.Members.Where(m => m.DeletedAt == null))
            .OrderBy(c => c.Name)
            .Select(c => new CreditCardDto
            {
                CreditCardId = c.CreditCardId,
                Name = c.Name,
                CurrencyCode = c.CurrencyCode,
                IsActive = c.IsActive,
                Members = c.Members.OrderByDescending(m => m.IsPrimary).Select(m => new CreditCardMemberDto
                {
                    CreditCardMemberId = m.CreditCardMemberId,
                    HolderName = m.HolderName,
                    LastFourDigits = m.LastFourDigits,
                    IsPrimary = m.IsPrimary,
                    IsActive = m.IsActive,
                    ExpirationMonth = m.ExpirationMonth,
                    ExpirationYear = m.ExpirationYear,
                    MemberId = m.MemberId,
                    IsCurrentUser = currentMemberId != null && m.MemberId == currentMemberId,
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
