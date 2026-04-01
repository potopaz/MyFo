using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.CreditCards.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.CreditCards.Commands;

public class UpdateCreditCardCommandHandler : IRequestHandler<UpdateCreditCardCommand, CreditCardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCreditCardCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreditCardDto> Handle(UpdateCreditCardCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.CreditCards
            .Include(c => c.Members)
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.CreditCardId == request.CreditCardId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("CreditCard", request.CreditCardId);

        if (entity.CurrencyCode != request.CurrencyCode)
        {
            var hasMovements = await _db.MovementPayments
                .AnyAsync(mp => mp.CreditCardId == entity.CreditCardId, cancellationToken);
            var hasStatements = await _db.StatementPeriods
                .AnyAsync(sp => sp.CreditCardId == entity.CreditCardId, cancellationToken);
            var hasCCPayments = await _db.CreditCardPayments
                .AnyAsync(cp => cp.CreditCardId == entity.CreditCardId, cancellationToken);
            var hasFrequentMovements = await _db.FrequentMovements
                .AnyAsync(fm => fm.CreditCardId == entity.CreditCardId, cancellationToken);
            if (hasMovements || hasStatements || hasCCPayments || hasFrequentMovements)
                throw new DomainException("HAS_OPERATIONS", "No se puede modificar la moneda porque la tarjeta tiene operaciones asociadas.");
        }

        entity.Name = request.Name;
        entity.CurrencyCode = request.CurrencyCode;
        entity.IsActive = request.IsActive;

        // Update primary member if holder data is provided
        if (request.HolderName is not null)
        {
            var primaryMember = entity.Members.FirstOrDefault(m => m.IsPrimary);
            if (primaryMember is not null)
            {
                primaryMember.HolderName = request.HolderName;
                primaryMember.LastFourDigits = string.IsNullOrWhiteSpace(request.LastFourDigits) ? null : request.LastFourDigits;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreditCardDto
        {
            CreditCardId = entity.CreditCardId,
            Name = entity.Name,
            CurrencyCode = entity.CurrencyCode,
            IsActive = entity.IsActive,
            Members = entity.Members.Select(m => new CreditCardMemberDto
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
