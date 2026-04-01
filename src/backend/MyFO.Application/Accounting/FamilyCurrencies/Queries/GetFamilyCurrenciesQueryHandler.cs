using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;
using MyFO.Application.Common.Interfaces;

namespace MyFO.Application.Accounting.FamilyCurrencies.Queries;

public class GetFamilyCurrenciesQueryHandler : IRequestHandler<GetFamilyCurrenciesQuery, List<FamilyCurrencyDto>>
{
    private readonly IApplicationDbContext _db;

    public GetFamilyCurrenciesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<FamilyCurrencyDto>> Handle(GetFamilyCurrenciesQuery request, CancellationToken cancellationToken)
    {
        return await _db.FamilyCurrencies
            .Include(fc => fc.Currency)
            .OrderBy(fc => fc.Currency.Code)
            .Select(fc => new FamilyCurrencyDto
            {
                FamilyCurrencyId = fc.FamilyCurrencyId,
                CurrencyId = fc.CurrencyId,
                Code = fc.Currency.Code,
                Name = fc.Currency.Name,
                Symbol = fc.Currency.Symbol,
                DecimalPlaces = fc.Currency.DecimalPlaces,
                IsActive = fc.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
