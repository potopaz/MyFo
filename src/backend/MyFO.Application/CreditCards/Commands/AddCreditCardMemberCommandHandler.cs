using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.DTOs;
using MyFO.Domain.CreditCards;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.Commands;

public class AddCreditCardMemberCommandHandler : IRequestHandler<AddCreditCardMemberCommand, CreditCardMemberDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddCreditCardMemberCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreditCardMemberDto> Handle(AddCreditCardMemberCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var card = await _db.CreditCards
            .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.CreditCardId == request.CreditCardId, cancellationToken)
            ?? throw new NotFoundException("CreditCard", request.CreditCardId);

        if (string.IsNullOrWhiteSpace(request.HolderName))
            throw new DomainException("INVALID_HOLDER_NAME", "El nombre del titular es requerido.");

        if (request.IsPrimary)
        {
            var alreadyHasPrimary = await _db.CreditCardMembers
                .AnyAsync(m => m.FamilyId == familyId
                    && m.CreditCardId == request.CreditCardId
                    && m.IsPrimary
                    && m.DeletedAt == null, cancellationToken);
            if (alreadyHasPrimary)
                throw new DomainException("DUPLICATE_PRIMARY", "La tarjeta ya tiene un titular principal. Solo puede haber uno.");
        }

        if (request.ExpirationMonth.HasValue && (request.ExpirationMonth < 1 || request.ExpirationMonth > 12))
            throw new DomainException("INVALID_EXPIRATION_MONTH", "El mes de vencimiento debe estar entre 1 y 12.");

        if (request.MemberId.HasValue)
        {
            var familyMemberExists = await _db.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.MemberId == request.MemberId.Value, cancellationToken);
            if (!familyMemberExists)
                throw new DomainException("INVALID_MEMBER", "El miembro de familia no existe.");
        }

        var member = new CreditCardMember
        {
            FamilyId = familyId,
            CreditCardMemberId = Guid.NewGuid(),
            CreditCardId = card.CreditCardId,
            HolderName = request.HolderName.Trim(),
            LastFourDigits = string.IsNullOrWhiteSpace(request.LastFourDigits) ? null : request.LastFourDigits.Trim(),
            IsPrimary = request.IsPrimary,
            IsActive = true,
            ExpirationMonth = request.ExpirationMonth,
            ExpirationYear = request.ExpirationYear,
            MemberId = request.MemberId,
        };

        await _db.CreditCardMembers.AddAsync(member, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreditCardMemberDto
        {
            CreditCardMemberId = member.CreditCardMemberId,
            HolderName = member.HolderName,
            LastFourDigits = member.LastFourDigits,
            IsPrimary = member.IsPrimary,
            IsActive = member.IsActive,
            ExpirationMonth = member.ExpirationMonth,
            ExpirationYear = member.ExpirationYear,
            MemberId = member.MemberId,
        };
    }
}
