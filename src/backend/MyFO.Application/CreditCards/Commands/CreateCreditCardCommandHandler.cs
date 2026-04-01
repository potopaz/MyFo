using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.DTOs;
using MyFO.Domain.CreditCards;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.Commands;

public class CreateCreditCardCommandHandler : IRequestHandler<CreateCreditCardCommand, CreditCardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCreditCardCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreditCardDto> Handle(CreateCreditCardCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Check for existing (including soft-deleted) with same name
        var existing = await _db.CreditCards
            .IgnoreQueryFilters()
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.Name == request.Name, cancellationToken);

        CreditCard card;

        if (existing is not null)
        {
            if (existing.DeletedAt is null)
                throw new DomainException("DUPLICATE_NAME", $"Ya existe una tarjeta con el nombre '{request.Name}'.");

            // Reactivate soft-deleted record and update data
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.CurrencyCode = request.CurrencyCode.ToUpperInvariant();
            existing.IsActive = true;

            // Update members
            if (request.Members is { Count: > 0 })
            {
                // Clear old members and add new ones
                existing.Members.Clear();
                foreach (var m in request.Members)
                {
                    existing.Members.Add(new CreditCardMember
                    {
                        FamilyId = familyId,
                        CreditCardMemberId = Guid.NewGuid(),
                        CreditCardId = existing.CreditCardId,
                        HolderName = m.HolderName,
                        LastFourDigits = m.LastFourDigits,
                        IsPrimary = m.IsPrimary
                    });
                }
            }
            card = existing;
        }
        else
        {
            card = new CreditCard
            {
                FamilyId = familyId,
                CreditCardId = Guid.NewGuid(),
                Name = request.Name,
                CurrencyCode = request.CurrencyCode.ToUpperInvariant()
            };

            if (request.Members is { Count: > 0 })
            {
                foreach (var m in request.Members)
                {
                    card.Members.Add(new CreditCardMember
                    {
                        FamilyId = familyId,
                        CreditCardMemberId = Guid.NewGuid(),
                        CreditCardId = card.CreditCardId,
                        HolderName = m.HolderName,
                        LastFourDigits = m.LastFourDigits,
                        IsPrimary = m.IsPrimary
                    });
                }
            }
            await _db.CreditCards.AddAsync(card, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreditCardDto
        {
            CreditCardId = card.CreditCardId,
            Name = card.Name,
            CurrencyCode = card.CurrencyCode,
            IsActive = card.IsActive,
            Members = card.Members.Select(m => new CreditCardMemberDto
            {
                CreditCardMemberId = m.CreditCardMemberId,
                HolderName = m.HolderName,
                LastFourDigits = m.LastFourDigits,
                IsPrimary = m.IsPrimary,
                IsActive = m.IsActive,
                ExpirationMonth = m.ExpirationMonth,
                ExpirationYear = m.ExpirationYear
            }).ToList()
        };
    }
}
