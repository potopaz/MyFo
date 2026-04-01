using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.Currencies.DTOs;
using MyFO.Application.Common.Interfaces;

namespace MyFO.Application.Accounting.Currencies.Queries;

public class GetCurrenciesQueryHandler : IRequestHandler<GetCurrenciesQuery, List<CurrencyDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCurrenciesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Currencies
            .OrderBy(c => c.Code)
            .Select(c => new CurrencyDto
            {
                CurrencyId = c.CurrencyId,
                Code = c.Code,
                Name = c.Name,
                Symbol = c.Symbol,
                DecimalPlaces = c.DecimalPlaces
            })
            .ToListAsync(cancellationToken);
    }
}
