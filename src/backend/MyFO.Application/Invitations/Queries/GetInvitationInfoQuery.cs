using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Invitations.DTOs;

namespace MyFO.Application.Invitations.Queries;

public record GetInvitationInfoQuery(string Token) : IRequest<InvitationInfoDto>;

public class GetInvitationInfoQueryHandler : IRequestHandler<GetInvitationInfoQuery, InvitationInfoDto>
{
    private readonly IApplicationDbContext _db;

    public GetInvitationInfoQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<InvitationInfoDto> Handle(GetInvitationInfoQuery request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: no JWT auth on this endpoint, so tenant filter would return nothing.
        // We manually filter by DeletedAt to respect soft-delete.
        var invitation = await _db.FamilyInvitations
            .IgnoreQueryFilters()
            .Where(i => i.Token == request.Token && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (invitation is null)
            return new InvitationInfoDto { IsValid = false, ErrorCode = "NOT_FOUND" };

        if (invitation.AcceptedAt.HasValue)
            return new InvitationInfoDto { IsValid = false, ErrorCode = "ALREADY_USED" };

        if (invitation.ExpiresAt < DateTime.UtcNow)
            return new InvitationInfoDto { IsValid = false, ErrorCode = "EXPIRED" };

        var family = await _db.Families
            .IgnoreQueryFilters()
            .Where(f => f.FamilyId == invitation.FamilyId && f.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (family is null)
            return new InvitationInfoDto { IsValid = false, ErrorCode = "FAMILY_NOT_FOUND" };

        return new InvitationInfoDto
        {
            FamilyName = family.Name,
            InvitedByDisplayName = invitation.InvitedByDisplayName,
            ExpiresAt = invitation.ExpiresAt,
            IsValid = true
        };
    }
}
