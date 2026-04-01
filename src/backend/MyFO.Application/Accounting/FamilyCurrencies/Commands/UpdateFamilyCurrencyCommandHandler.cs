using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public class UpdateFamilyCurrencyCommandHandler : IRequestHandler<UpdateFamilyCurrencyCommand, FamilyCurrencyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateFamilyCurrencyCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FamilyCurrencyDto> Handle(UpdateFamilyCurrencyCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var entity = await _db.FamilyCurrencies
            .Include(fc => fc.Currency)
            .FirstOrDefaultAsync(x => x.FamilyId == familyId
                                   && x.FamilyCurrencyId == request.FamilyCurrencyId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("FamilyCurrency", request.FamilyCurrencyId);

        // Validate deactivation of protected currencies
        if (!request.IsActive && entity.IsActive)
        {
            var currencyCode = entity.Currency.Code;

            var family = await _db.Families
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.FamilyId == familyId && f.DeletedAt == null, cancellationToken);

            if (family is not null)
            {
                if (string.Equals(family.PrimaryCurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                    throw new DomainException("CURRENCY_PROTECTED",
                        $"No se puede desactivar '{currencyCode}' porque es la moneda principal de la familia.");

                if (string.Equals(family.SecondaryCurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                    throw new DomainException("CURRENCY_PROTECTED",
                        $"No se puede desactivar '{currencyCode}' porque es la moneda secundaria de la familia.");
            }
        }

        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return new FamilyCurrencyDto
        {
            FamilyCurrencyId = entity.FamilyCurrencyId,
            CurrencyId = entity.CurrencyId,
            Code = entity.Currency.Code,
            Name = entity.Currency.Name,
            Symbol = entity.Currency.Symbol,
            DecimalPlaces = entity.Currency.DecimalPlaces,
            IsActive = entity.IsActive
        };
    }
}
