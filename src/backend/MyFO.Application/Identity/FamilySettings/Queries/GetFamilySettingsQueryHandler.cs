using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Identity.FamilySettings.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Identity.FamilySettings.Queries;

public class GetFamilySettingsQueryHandler : IRequestHandler<GetFamilySettingsQuery, FamilySettingsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFamilySettingsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FamilySettingsDto> Handle(GetFamilySettingsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var family = await _db.Families
            .IgnoreQueryFilters()
            .Where(f => f.FamilyId == familyId && f.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Family", familyId);

        var hasTransactions = await _db.Movements.AnyAsync(cancellationToken)
                           || await _db.Transfers.AnyAsync(cancellationToken);

        return new FamilySettingsDto
        {
            Name = family.Name,
            PrimaryCurrencyCode = family.PrimaryCurrencyCode,
            SecondaryCurrencyCode = family.SecondaryCurrencyCode,
            Language = family.Language,
            CanChangeCurrencies = !hasTransactions
        };
    }
}
