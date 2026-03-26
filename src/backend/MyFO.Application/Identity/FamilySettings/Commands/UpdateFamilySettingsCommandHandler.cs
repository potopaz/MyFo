using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Identity.FamilySettings.DTOs;
using MyFO.Domain.Accounting;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Identity.FamilySettings.Commands;

public class UpdateFamilySettingsCommandHandler : IRequestHandler<UpdateFamilySettingsCommand, FamilySettingsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateFamilySettingsCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FamilySettingsDto> Handle(UpdateFamilySettingsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var family = await _db.Families
            .IgnoreQueryFilters()
            .Where(f => f.FamilyId == familyId && f.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Family", familyId);

        // Validate currency changes are allowed if transactions exist
        var primaryChanged   = !string.Equals(family.PrimaryCurrencyCode, request.PrimaryCurrencyCode, StringComparison.OrdinalIgnoreCase);
        var secondaryChanged = !string.Equals(family.SecondaryCurrencyCode, request.SecondaryCurrencyCode, StringComparison.OrdinalIgnoreCase);

        if (primaryChanged || secondaryChanged)
        {
            var hasTransactions = await _db.Movements.AnyAsync(cancellationToken)
                               || await _db.Transfers.AnyAsync(cancellationToken);
            if (hasTransactions)
                throw new DomainException("CURRENCIES_LOCKED", "No se pueden cambiar las monedas porque ya existen movimientos o transferencias registrados.");
        }

        // Validate primary currency
        var primaryCurrency = await _db.Currencies
            .IgnoreQueryFilters()
            .Where(c => c.Code == request.PrimaryCurrencyCode && c.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("INVALID_CURRENCY", $"La moneda primaria '{request.PrimaryCurrencyCode}' no existe.");

        // Validate secondary currency (always required)
        if (string.IsNullOrWhiteSpace(request.SecondaryCurrencyCode))
            throw new DomainException("MISSING_SECONDARY_CURRENCY", "La moneda secundaria es requerida.");

        var secondaryCurrency = await _db.Currencies
            .IgnoreQueryFilters()
            .Where(c => c.Code == request.SecondaryCurrencyCode && c.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("INVALID_CURRENCY", $"La moneda secundaria '{request.SecondaryCurrencyCode}' no existe.");

        // Update family fields
        family.Name = request.Name;
        family.PrimaryCurrencyCode = request.PrimaryCurrencyCode;
        family.SecondaryCurrencyCode = request.SecondaryCurrencyCode;
        family.Language = request.Language;

        // Auto-associate currencies
        await EnsureFamilyCurrencyExists(familyId, primaryCurrency.CurrencyId, cancellationToken);
        await EnsureFamilyCurrencyExists(familyId, secondaryCurrency.CurrencyId, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new FamilySettingsDto
        {
            Name = family.Name,
            PrimaryCurrencyCode = family.PrimaryCurrencyCode,
            SecondaryCurrencyCode = family.SecondaryCurrencyCode,
            Language = family.Language
        };
    }

    private async Task EnsureFamilyCurrencyExists(Guid familyId, Guid currencyId, CancellationToken cancellationToken)
    {
        var alreadyAssociated = await _db.FamilyCurrencies
            .AnyAsync(fc => fc.FamilyId == familyId && fc.CurrencyId == currencyId, cancellationToken);

        if (!alreadyAssociated)
        {
            await _db.FamilyCurrencies.AddAsync(new FamilyCurrency
            {
                FamilyId = familyId,
                FamilyCurrencyId = Guid.NewGuid(),
                CurrencyId = currencyId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId
            }, cancellationToken);
        }
    }
}
