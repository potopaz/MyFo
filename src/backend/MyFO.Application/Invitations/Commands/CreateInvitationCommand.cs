using System.Security.Cryptography;
using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Invitations.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Identity;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Invitations.Commands;

public class CreateInvitationCommand : IRequest<CreateInvitationResponse>
{
    public string Email { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

public class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, CreateInvitationResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly IUserQueryService _userQueryService;

    public CreateInvitationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IEmailService emailService, IUserQueryService userQueryService)
    {
        _db = db;
        _currentUser = currentUser;
        _emailService = emailService;
        _userQueryService = userQueryService;
    }

    public async Task<CreateInvitationResponse> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("EMAIL_REQUIRED", "El email es requerido.");

        var email = request.Email.Trim().ToLowerInvariant();
        var familyId = _currentUser.FamilyId.Value;

        // Check if the email already belongs to a family member (active or inactive)
        var existingUserId = await _userQueryService.FindUserIdByEmailAsync(email, cancellationToken);
        if (existingUserId.HasValue)
        {
            var isMember = await _db.FamilyMembers
                .AnyAsync(m => m.UserId == existingUserId.Value, cancellationToken);
            if (isMember)
                throw new DomainException("MEMBER_ALREADY_EXISTS", "Este email ya pertenece a un miembro de la familia.");
        }

        // Check if there's already a pending invitation for this email
        var existingInvitation = await _db.FamilyInvitations
            .FirstOrDefaultAsync(i => i.InvitedEmail == email
                && i.FamilyId == familyId
                && i.AcceptedAt == null
                && i.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (existingInvitation != null)
            throw new DomainException("INVITATION_ALREADY_EXISTS", "Ya existe una invitación pendiente para este email.");

        // Check MaxMembers
        var adminConfig = await _db.FamilyAdminConfigs
            .FirstOrDefaultAsync(c => c.FamilyId == familyId, cancellationToken);

        if (adminConfig?.MaxMembers != null)
        {
            var activeCount = await _db.FamilyMembers
                .CountAsync(m => m.IsActive, cancellationToken);

            if (activeCount >= adminConfig.MaxMembers.Value)
                throw new DomainException("MAX_MEMBERS_REACHED",
                    $"Se alcanzó el límite máximo de {adminConfig.MaxMembers.Value} miembros activos.");
        }

        // Get inviter's display name and family name
        var inviterMember = await _db.FamilyMembers
            .Where(m => m.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("No se encontró el miembro actual.");

        var family = await _db.Families
            .FirstOrDefaultAsync(f => f.FamilyId == familyId, cancellationToken)
            ?? throw new ForbiddenException("No se encontró la familia.");

        // Generate URL-safe token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var invitation = new FamilyInvitation
        {
            InvitationId = Guid.NewGuid(),
            FamilyId = familyId,
            Token = token,
            InvitedByDisplayName = inviterMember.DisplayName,
            InvitedEmail = email,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        await _db.FamilyInvitations.AddAsync(invitation, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        // Send invitation email (best effort — don't fail the whole operation)
        try
        {
            var subject = $"{inviterMember.DisplayName} te invitó a {family.Name} en MyFO";
            var htmlBody = $@"
                <div style='font-family: sans-serif; max-width: 480px; margin: 0 auto;'>
                    <h2>Te invitaron a MyFO</h2>
                    <p><strong>{inviterMember.DisplayName}</strong> te invitó a unirte a la familia <strong>{family.Name}</strong> en MyFO.</p>
                    <p>Hacé clic en el siguiente enlace para aceptar la invitación:</p>
                    <p><a href='{request.BaseUrl}/join?token={token}' style='display:inline-block;padding:10px 24px;background:#2563eb;color:#fff;text-decoration:none;border-radius:6px;'>Aceptar invitación</a></p>
                    <p style='color:#666;font-size:13px;'>Este enlace vence en 48 horas.</p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, htmlBody, cancellationToken);
        }
        catch
        {
            // Email sending failed — invitation is still created, link can be shared manually
        }

        return new CreateInvitationResponse
        {
            Token = token,
            ExpiresAt = invitation.ExpiresAt
        };
    }
}
