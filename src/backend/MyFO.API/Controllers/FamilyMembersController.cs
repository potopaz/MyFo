using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Identity.Enums;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/family-members")]
public class FamilyMembersController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FamilyMembersController(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns all members (active and inactive) with full details.
    /// FamilyAdmin sees all; Member sees only active members (simple list).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FamilyMemberDto>>> GetAll(CancellationToken ct)
    {
        if (_currentUser.FamilyId is null)
            return Forbid();

        if (_currentUser.IsFamilyAdmin)
        {
            var members = await _db.FamilyMembers
                .OrderBy(m => m.DisplayName)
                .Select(m => new FamilyMemberDto
                {
                    MemberId = m.MemberId,
                    UserId = m.UserId,
                    DisplayName = m.DisplayName,
                    Role = m.Role.ToString(),
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(members);
        }
        else
        {
            // Members only see active members (simple list for selects, etc.)
            var members = await _db.FamilyMembers
                .Where(m => m.IsActive)
                .OrderBy(m => m.DisplayName)
                .Select(m => new FamilyMemberDto
                {
                    MemberId = m.MemberId,
                    UserId = m.UserId,
                    DisplayName = m.DisplayName,
                    Role = m.Role.ToString(),
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(members);
        }
    }

    /// <summary>
    /// Toggle a member's active status. FamilyAdmin only.
    /// Cannot deactivate yourself.
    /// </summary>
    [HttpPut("{memberId}/toggle-active")]
    public async Task<ActionResult> ToggleActive(Guid memberId, CancellationToken ct)
    {
        if (_currentUser.FamilyId is null)
            return Forbid();

        if (!_currentUser.IsFamilyAdmin)
            return Forbid();

        var member = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct)
            ?? throw new NotFoundException("FamilyMember", memberId);

        if (member.UserId == _currentUser.UserId)
            throw new DomainException("CANNOT_DEACTIVATE_SELF", "No podés desactivarte a vos mismo.");

        // Check MaxMembers when reactivating
        if (!member.IsActive)
        {
            var adminConfig = await _db.FamilyAdminConfigs
                .FirstOrDefaultAsync(c => c.FamilyId == _currentUser.FamilyId.Value, ct);

            if (adminConfig?.MaxMembers != null)
            {
                var activeCount = await _db.FamilyMembers
                    .CountAsync(m => m.IsActive, ct);

                if (activeCount >= adminConfig.MaxMembers.Value)
                    throw new DomainException("MAX_MEMBERS_REACHED",
                        $"Se alcanzó el límite máximo de {adminConfig.MaxMembers.Value} miembros activos.");
            }
        }

        member.IsActive = !member.IsActive;
        await _db.SaveChangesAsync(ct);

        return Ok(new { member.IsActive });
    }

    /// <summary>
    /// Change a member's role. FamilyAdmin only.
    /// Cannot change your own role. Must keep at least one FamilyAdmin.
    /// </summary>
    [HttpPut("{memberId}/role")]
    public async Task<ActionResult> ChangeRole(Guid memberId, [FromBody] ChangeRoleRequest request, CancellationToken ct)
    {
        if (_currentUser.FamilyId is null)
            return Forbid();

        if (!_currentUser.IsFamilyAdmin)
            return Forbid();

        if (!Enum.TryParse<UserRole>(request.Role, out var newRole))
            throw new DomainException("INVALID_ROLE", "Rol inválido.");

        var member = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct)
            ?? throw new NotFoundException("FamilyMember", memberId);

        if (member.UserId == _currentUser.UserId)
            throw new DomainException("CANNOT_CHANGE_OWN_ROLE", "No podés cambiar tu propio rol.");

        // If demoting from FamilyAdmin, ensure at least one admin remains
        if (member.Role == UserRole.FamilyAdmin && newRole == UserRole.Member)
        {
            var adminCount = await _db.FamilyMembers
                .CountAsync(m => m.Role == UserRole.FamilyAdmin && m.IsActive, ct);

            if (adminCount <= 1)
                throw new DomainException("LAST_ADMIN", "Debe haber al menos un administrador activo en la familia.");
        }

        member.Role = newRole;
        await _db.SaveChangesAsync(ct);

        return Ok(new { Role = newRole.ToString() });
    }

    /// <summary>
    /// List pending (not accepted, not expired) invitations. FamilyAdmin only.
    /// </summary>
    [HttpGet("invitations")]
    public async Task<ActionResult<List<InvitationListDto>>> GetPendingInvitations(CancellationToken ct)
    {
        if (_currentUser.FamilyId is null)
            return Forbid();

        if (!_currentUser.IsFamilyAdmin)
            return Forbid();

        var now = DateTime.UtcNow;
        var invitations = await _db.FamilyInvitations
            .Where(i => i.AcceptedAt == null && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationListDto
            {
                InvitationId = i.InvitationId,
                InvitedByDisplayName = i.InvitedByDisplayName,
                InvitedEmail = i.InvitedEmail,
                ExpiresAt = i.ExpiresAt,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(invitations);
    }
}

public class FamilyMemberDto
{
    public Guid MemberId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class InvitationListDto
{
    public Guid InvitationId { get; set; }
    public string InvitedByDisplayName { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
