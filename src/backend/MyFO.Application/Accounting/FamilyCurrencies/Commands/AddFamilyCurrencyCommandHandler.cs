using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public class AddFamilyCurrencyCommandHandler : IRequestHandler<AddFamilyCurrencyCommand, FamilyCurrencyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AddFamilyCurrencyCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FamilyCurrencyDto> Handle(AddFamilyCurrencyCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var normalizedCode = request.CurrencyCode.Trim().ToUpperInvariant();

        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken)
            ?? throw new DomainException("CURRENCY_NOT_FOUND", $"La moneda '{normalizedCode}' no existe.");

        // Check including soft-deleted records (unique index has no deleted_at filter)
        var existing = await _db.FamilyCurrencies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(fc => fc.FamilyId == _currentUser.FamilyId.Value
                                    && fc.CurrencyId == currency.CurrencyId, cancellationToken);

        FamilyCurrency familyCurrency;

        if (existing is not null)
        {
            if (existing.DeletedAt is null)
                throw new DomainException("CURRENCY_ALREADY_ADDED", $"La moneda '{currency.Code}' ya está asociada a esta familia.");

            // Reactivate soft-deleted record
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.IsActive = true;
            familyCurrency = existing;
        }
        else
        {
            familyCurrency = new FamilyCurrency
            {
                FamilyId = _currentUser.FamilyId.Value,
                FamilyCurrencyId = Guid.NewGuid(),
                CurrencyId = currency.CurrencyId
            };
            await _db.FamilyCurrencies.AddAsync(familyCurrency, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new FamilyCurrencyDto
        {
            FamilyCurrencyId = familyCurrency.FamilyCurrencyId,
            CurrencyId = currency.CurrencyId,
            Code = currency.Code,
            Name = currency.Name,
            Symbol = currency.Symbol,
            DecimalPlaces = currency.DecimalPlaces,
            IsActive = familyCurrency.IsActive
        };
    }
}
