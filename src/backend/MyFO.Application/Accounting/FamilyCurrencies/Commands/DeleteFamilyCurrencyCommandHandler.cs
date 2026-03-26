using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public class DeleteFamilyCurrencyCommandHandler : IRequestHandler<DeleteFamilyCurrencyCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteFamilyCurrencyCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteFamilyCurrencyCommand request, CancellationToken cancellationToken)
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

        var currencyCode = entity.Currency.Code;

        // Check if it's the family's primary or secondary currency
        var family = await _db.Families
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.FamilyId == familyId && f.DeletedAt == null, cancellationToken);

        if (family is not null)
        {
            if (string.Equals(family.PrimaryCurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("CURRENCY_IN_USE",
                    $"No se puede eliminar '{currencyCode}' porque es la moneda principal de la familia.");

            if (string.Equals(family.SecondaryCurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("CURRENCY_IN_USE",
                    $"No se puede eliminar '{currencyCode}' porque es la moneda secundaria de la familia.");
        }

        // Check cash boxes using this currency
        var hasCashBoxes = await _db.CashBoxes
            .AnyAsync(c => c.CurrencyCode == currencyCode, cancellationToken);
        if (hasCashBoxes)
            throw new DomainException("CURRENCY_IN_USE",
                $"No se puede eliminar '{currencyCode}' porque tiene cajas asociadas.");

        // Check bank accounts using this currency
        var hasBankAccounts = await _db.BankAccounts
            .AnyAsync(b => b.CurrencyCode == currencyCode, cancellationToken);
        if (hasBankAccounts)
            throw new DomainException("CURRENCY_IN_USE",
                $"No se puede eliminar '{currencyCode}' porque tiene cuentas bancarias asociadas.");

        // Check credit cards using this currency
        var hasCreditCards = await _db.CreditCards
            .AnyAsync(cc => cc.CurrencyCode == currencyCode, cancellationToken);
        if (hasCreditCards)
            throw new DomainException("CURRENCY_IN_USE",
                $"No se puede eliminar '{currencyCode}' porque tiene tarjetas de crédito asociadas.");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
