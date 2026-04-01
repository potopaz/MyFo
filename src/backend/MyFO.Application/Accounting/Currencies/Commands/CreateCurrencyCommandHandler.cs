using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.Currencies.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting;
using MyFO.Domain.Exceptions;

namespace MyFO.Application.Accounting.Currencies.Commands;

public class CreateCurrencyCommandHandler : IRequestHandler<CreateCurrencyCommand, CurrencyDto>
{
    private readonly IApplicationDbContext _db;

    public CreateCurrencyCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CurrencyDto> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();

        var exists = await _db.Currencies
            .AnyAsync(c => c.Code == code, cancellationToken);

        if (exists)
            throw new DomainException("CURRENCY_EXISTS", $"Ya existe la moneda '{code}'.");

        var currency = new Currency
        {
            CurrencyId = Guid.NewGuid(),
            Code = code,
            Name = request.Name,
            Symbol = request.Symbol,
            DecimalPlaces = request.DecimalPlaces
        };

        await _db.Currencies.AddAsync(currency, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new CurrencyDto
        {
            CurrencyId = currency.CurrencyId,
            Code = currency.Code,
            Name = currency.Name,
            Symbol = currency.Symbol,
            DecimalPlaces = currency.DecimalPlaces
        };
    }
}
