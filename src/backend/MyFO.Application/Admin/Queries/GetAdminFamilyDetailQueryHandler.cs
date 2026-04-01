using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Admin.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;

namespace MyFO.Application.Admin.Queries;

public class GetAdminFamilyDetailQueryHandler : IRequestHandler<GetAdminFamilyDetailQuery, AdminFamilyDetailDto>
{
    private readonly IAdminDbContext _db;

    public GetAdminFamilyDetailQueryHandler(IAdminDbContext db) => _db = db;

    public async Task<AdminFamilyDetailDto> Handle(GetAdminFamilyDetailQuery request, CancellationToken cancellationToken)
    {
        var family = await _db.Families
                        .FirstOrDefaultAsync(f => f.FamilyId == request.FamilyId && f.DeletedAt == null, cancellationToken);

        if (family is null)
            throw new NotFoundException("Family", request.FamilyId);

        var config = await _db.FamilyAdminConfigs
                        .FirstOrDefaultAsync(c => c.FamilyId == request.FamilyId && c.DeletedAt == null, cancellationToken);

        var members = await _db.FamilyMembers
                        .Where(m => m.FamilyId == request.FamilyId && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        return new AdminFamilyDetailDto
        {
            FamilyId = family.FamilyId,
            Name = family.Name,
            PrimaryCurrencyCode = family.PrimaryCurrencyCode,
            SecondaryCurrencyCode = family.SecondaryCurrencyCode,
            Language = family.Language,
            CreatedAt = family.CreatedAt,
            MemberCount = members.Count(m => m.IsActive),
            IsEnabled = config?.IsEnabled ?? true,
            MaxMembers = config?.MaxMembers,
            Notes = config?.Notes,
            DisabledAt = config?.DisabledAt,
            DisabledReason = config?.DisabledReason,
            Members = members.Select(m => new AdminFamilyMemberDto
            {
                MemberId = m.MemberId,
                DisplayName = m.DisplayName,
                Role = m.Role.ToString(),
                IsActive = m.IsActive,
            }).ToList(),
        };
    }
}
