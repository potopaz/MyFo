using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyFO.Application.Auth;
using MyFO.Application.Auth.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Identity;
using MyFO.Domain.Identity.Enums;
using MyFO.Infrastructure.Email;
using MyFO.Infrastructure.Identity;
using MyFO.Infrastructure.Persistence;

namespace MyFO.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly IVerificationTokenService _verificationTokenService;
    private readonly IEmailService _emailService;
    private readonly string _frontendUrl;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IOptions<JwtSettings> jwtSettings,
        IVerificationTokenService verificationTokenService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
        _verificationTokenService = verificationTokenService;
        _emailService = emailService;
        _frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
    }

    // ==================== NEW AUTH FLOW ====================

    public async Task<CheckEmailResponse> CheckEmailAsync(CheckEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        return new CheckEmailResponse { Exists = user is not null };
    }

    public async Task InitiateRegistrationAsync(InitiateRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new DomainException("EMAIL_TAKEN", "Ya existe una cuenta con este email.");

        // Pre-hash the password (we store the hash in the token, never the plain password)
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var passwordHash = passwordHasher.HashPassword(null!, request.Password);

        // Generate signed verification token with user data
        var verificationToken = _verificationTokenService.GenerateRegistrationToken(
            request.Email, request.FullName, passwordHash, request.Language);

        // Build verification URL
        var verifyUrl = $"{_frontendUrl}/auth/verify?token={Uri.EscapeDataString(verificationToken)}";

        // Send verification email
        var html = EmailTemplates.VerifyEmail(request.FullName, verifyUrl);
        await _emailService.SendEmailAsync(request.Email, "Verificá tu email - MyFO", html, cancellationToken);
    }

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        // Validate and extract data from the signed token
        var data = _verificationTokenService.ValidateRegistrationToken(request.Token)
            ?? throw new DomainException("INVALID_TOKEN", "El link de verificación es inválido o ha expirado.");

        // Check email not taken (could happen if someone registers between initiate and verify)
        var existingUser = await _userManager.FindByEmailAsync(data.Email);
        if (existingUser is not null)
            throw new DomainException("EMAIL_TAKEN", "Ya existe una cuenta con este email.");

        // Create the user with the pre-hashed password
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = data.Email,
            Email = data.Email,
            EmailConfirmed = true,
            FullName = data.FullName,
            Language = data.Language,
            CreatedAt = DateTime.UtcNow
        };

        // Set the pre-computed hash directly, then create without password
        user.PasswordHash = data.PasswordHash;
        var identityResult = await _userManager.CreateAsync(user);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            throw new DomainException("IDENTITY_ERROR", errors);
        }

        // Generate limited JWT (no family_id)
        var token = GenerateJwtToken(user, null, null);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            IsSuperAdmin = user.IsSuperAdmin,
            Families = []
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new DomainException("INVALID_CREDENTIALS", "Email o contraseña incorrectos.");

        if (user.DeletedAt.HasValue)
            throw new DomainException("ACCOUNT_DISABLED", "Esta cuenta ha sido deshabilitada.");

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
            throw new DomainException("INVALID_CREDENTIALS", "Email o contraseña incorrectos.");

        // Open the connection explicitly so the SET and query use the SAME connection.
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        // Set RLS variable for user_membership_lookup policy
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SET app.current_user_id = '" + user.Id + "'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Find all families for this user
        var memberships = await _dbContext.FamilyMembers
            .IgnoreQueryFilters()
            .Include(m => m.Family)
            .Where(m => m.UserId == user.Id && m.IsActive && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Generate LIMITED JWT (no family_id) — family selection happens in a separate step
        var token = GenerateJwtToken(user, null, null);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            IsSuperAdmin = user.IsSuperAdmin,
            Families = memberships.Select(m => new UserFamilyDto
            {
                FamilyId = m.FamilyId,
                FamilyName = m.Family.Name,
                Role = m.Role.ToString()
            }).ToList()
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        // Always return silently to not reveal whether the email exists
        if (user is null || user.DeletedAt.HasValue)
            return;

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = $"{_frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(user.Email!)}";

        var html = EmailTemplates.ResetPassword(user.FullName, resetUrl);
        await _emailService.SendEmailAsync(user.Email!, "Recuperar contraseña - MyFO", html, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new DomainException("INVALID_TOKEN", "El link de recuperación es inválido.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new DomainException("RESET_ERROR", errors);
        }
    }

    public async Task<SelectFamilyResponse> SelectFamilyAsync(Guid userId, SelectFamilyRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        // Open connection explicitly for RLS
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SET app.current_user_id = '" + userId + "'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Verify user is a member of the requested family
        var membership = await _dbContext.FamilyMembers
            .IgnoreQueryFilters()
            .Include(m => m.Family)
            .Where(m => m.UserId == userId && m.FamilyId == request.FamilyId && m.IsActive && m.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("NOT_MEMBER", "No sos miembro de esta familia.");

        // Generate FULL JWT with family_id
        var token = GenerateJwtToken(user, membership.FamilyId, membership.Role);

        return new SelectFamilyResponse
        {
            Token = token,
            FamilyId = membership.FamilyId,
            FamilyName = membership.Family.Name,
            Role = membership.Role.ToString()
        };
    }

    public async Task<AuthResponse> CreateFamilyAsync(Guid userId, CreateFamilyRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Create the family
        var family = new Family
        {
            FamilyId = Guid.NewGuid(),
            Name = request.Name,
            PrimaryCurrencyCode = request.PrimaryCurrencyCode.ToUpperInvariant(),
            SecondaryCurrencyCode = request.SecondaryCurrencyCode.ToUpperInvariant(),
            Language = user.Language,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _dbContext.Families.AddAsync(family, cancellationToken);

        // Set RLS variable so the INSERT on family_members is allowed
#pragma warning disable EF1003
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SET app.current_family_id = '" + family.FamilyId + "'", cancellationToken);
#pragma warning restore EF1003

        // Create the family member (user as FamilyAdmin)
        var member = new FamilyMember
        {
            FamilyId = family.FamilyId,
            MemberId = Guid.NewGuid(),
            UserId = userId,
            Role = UserRole.FamilyAdmin,
            DisplayName = user.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _dbContext.FamilyMembers.AddAsync(member, cancellationToken);

        // Auto-associate primary and secondary currencies
        var currencyCodes = new List<string> { family.PrimaryCurrencyCode, family.SecondaryCurrencyCode };
        var currencies = await _dbContext.Currencies
            .Where(c => currencyCodes.Contains(c.Code))
            .ToListAsync(cancellationToken);

        foreach (var currency in currencies)
        {
            await _dbContext.FamilyCurrencies.AddAsync(new FamilyCurrency
            {
                FamilyId = family.FamilyId,
                FamilyCurrencyId = Guid.NewGuid(),
                CurrencyId = currency.CurrencyId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Now query ALL families (including the new one) for the response
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SET app.current_user_id = '" + userId + "'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        var allMemberships = await _dbContext.FamilyMembers
            .IgnoreQueryFilters()
            .Include(m => m.Family)
            .Where(m => m.UserId == userId && m.IsActive && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Generate limited JWT (user goes back to family selection)
        var token = GenerateJwtToken(user, null, null);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            IsSuperAdmin = user.IsSuperAdmin,
            Families = allMemberships.Select(m => new UserFamilyDto
            {
                FamilyId = m.FamilyId,
                FamilyName = m.Family.Name,
                Role = m.Role.ToString()
            }).ToList()
        };
    }

    // ==================== EXTERNAL (SOCIAL) AUTH ====================

    public async Task<AuthResponse> ExternalLoginAsync(string email, string fullName, string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            if (user.DeletedAt.HasValue)
                throw new DomainException("ACCOUNT_DISABLED", "Esta cuenta ha sido deshabilitada.");

            // Link external login if not already linked
            var existingLogins = await _userManager.GetLoginsAsync(user);
            if (!existingLogins.Any(l => l.LoginProvider == provider && l.ProviderKey == providerKey))
            {
                await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
            }

            // Ensure email is confirmed (social auth = verified)
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
        }
        else
        {
            // Create new user (no password, email verified)
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Language = "es",
                CreatedAt = DateTime.UtcNow
            };

            var identityResult = await _userManager.CreateAsync(user);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                throw new DomainException("IDENTITY_ERROR", errors);
            }

            await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
        }

        // Get families
        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SET app.current_user_id = '" + user.Id + "'";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        var memberships = await _dbContext.FamilyMembers
            .IgnoreQueryFilters()
            .Include(m => m.Family)
            .Where(m => m.UserId == user.Id && m.IsActive && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var token = GenerateJwtToken(user, null, null);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            IsSuperAdmin = user.IsSuperAdmin,
            Families = memberships.Select(m => new UserFamilyDto
            {
                FamilyId = m.FamilyId,
                FamilyName = m.Family.Name,
                Role = m.Role.ToString()
            }).ToList()
        };
    }

    // ==================== PROFILE ====================

    public async Task UpdateProfileAsync(string userId, string fullName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        user.FullName = fullName;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new DomainException("UPDATE_ERROR", string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            throw new DomainException("PASSWORD_ERROR", string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    // ==================== LEGACY (invitation flow) ====================

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new DomainException("EMAIL_TAKEN", "Ya existe una cuenta con este email.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Language = request.Language,
            CreatedAt = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            throw new DomainException("IDENTITY_ERROR", errors);
        }

        var family = new Family
        {
            FamilyId = Guid.NewGuid(),
            Name = request.FamilyName,
            PrimaryCurrencyCode = request.PrimaryCurrencyCode.ToUpperInvariant(),
            SecondaryCurrencyCode = request.SecondaryCurrencyCode.ToUpperInvariant(),
            Language = request.Language,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        };

        await _dbContext.Families.AddAsync(family, cancellationToken);

#pragma warning disable EF1003
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SET app.current_family_id = '" + family.FamilyId + "'", cancellationToken);
#pragma warning restore EF1003

        var member = new FamilyMember
        {
            FamilyId = family.FamilyId,
            MemberId = Guid.NewGuid(),
            UserId = user.Id,
            Role = UserRole.FamilyAdmin,
            DisplayName = request.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        };

        await _dbContext.FamilyMembers.AddAsync(member, cancellationToken);

        var currencyCodes = new List<string> { family.PrimaryCurrencyCode, family.SecondaryCurrencyCode };
        var currencies = await _dbContext.Currencies
            .Where(c => currencyCodes.Contains(c.Code))
            .ToListAsync(cancellationToken);

        foreach (var currency in currencies)
        {
            await _dbContext.FamilyCurrencies.AddAsync(new FamilyCurrency
            {
                FamilyId = family.FamilyId,
                FamilyCurrencyId = Guid.NewGuid(),
                CurrencyId = currency.CurrencyId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var token = GenerateJwtToken(user, family.FamilyId, member.Role);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            IsSuperAdmin = user.IsSuperAdmin,
            Families =
            [
                new UserFamilyDto
                {
                    FamilyId = family.FamilyId,
                    FamilyName = family.Name,
                    Role = member.Role.ToString()
                }
            ]
        };
    }

    public async Task<AuthResponse> RegisterWithInvitationAsync(RegisterWithInvitationRequest request, CancellationToken cancellationToken = default)
    {
        var invitation = await _dbContext.FamilyInvitations
            .IgnoreQueryFilters()
            .Where(i => i.Token == request.InvitationToken && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("INVITATION_NOT_FOUND", "La invitación no existe o ya no es válida.");

        if (invitation.AcceptedAt.HasValue)
            throw new DomainException("INVITATION_ALREADY_USED", "Esta invitación ya fue utilizada.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new DomainException("INVITATION_EXPIRED", "La invitación ha expirado.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new DomainException("EMAIL_TAKEN", "Ya existe una cuenta con este email.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            throw new DomainException("IDENTITY_ERROR", errors);
        }

#pragma warning disable EF1003
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SET app.current_family_id = '" + invitation.FamilyId + "'", cancellationToken);
#pragma warning restore EF1003

        var member = new FamilyMember
        {
            FamilyId = invitation.FamilyId,
            MemberId = Guid.NewGuid(),
            UserId = user.Id,
            Role = UserRole.Member,
            DisplayName = request.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        };

        await _dbContext.FamilyMembers.AddAsync(member, cancellationToken);

        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.AcceptedByUserId = user.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var family = await _dbContext.Families
            .IgnoreQueryFilters()
            .Where(f => f.FamilyId == invitation.FamilyId && f.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        var token = GenerateJwtToken(user, invitation.FamilyId, UserRole.Member);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            IsSuperAdmin = user.IsSuperAdmin,
            Families =
            [
                new UserFamilyDto
                {
                    FamilyId = invitation.FamilyId,
                    FamilyName = family?.Name ?? string.Empty,
                    Role = UserRole.Member.ToString()
                }
            ]
        };
    }

    public async Task<AuthResponse> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
    {
        var invitation = await _dbContext.FamilyInvitations
            .IgnoreQueryFilters()
            .Where(i => i.Token == token && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("INVITATION_NOT_FOUND", "La invitación no existe o ya no es válida.");

        if (invitation.AcceptedAt.HasValue)
            throw new DomainException("INVITATION_ALREADY_USED", "Esta invitación ya fue utilizada.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new DomainException("INVITATION_EXPIRED", "La invitación ha expirado.");

        var alreadyMember = await _dbContext.FamilyMembers
            .IgnoreQueryFilters()
            .AnyAsync(m => m.FamilyId == invitation.FamilyId && m.UserId == userId && m.DeletedAt == null, cancellationToken);

        if (alreadyMember)
            throw new DomainException("ALREADY_MEMBER", "Ya sos miembro de esta familia.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

#pragma warning disable EF1003
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SET app.current_family_id = '" + invitation.FamilyId + "'", cancellationToken);
#pragma warning restore EF1003

        var member = new FamilyMember
        {
            FamilyId = invitation.FamilyId,
            MemberId = Guid.NewGuid(),
            UserId = userId,
            Role = UserRole.Member,
            DisplayName = user.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _dbContext.FamilyMembers.AddAsync(member, cancellationToken);

        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.AcceptedByUserId = userId;

        await _dbContext.SaveChangesAsync(cancellationToken);

#pragma warning disable EF1003
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SET app.current_user_id = '" + userId + "'", cancellationToken);
#pragma warning restore EF1003

        var memberships = await _dbContext.FamilyMembers
            .IgnoreQueryFilters()
            .Include(m => m.Family)
            .Where(m => m.UserId == userId && m.IsActive && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var jwtToken = GenerateJwtToken(user, invitation.FamilyId, UserRole.Member);

        return new AuthResponse
        {
            Token = jwtToken,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            IsSuperAdmin = user.IsSuperAdmin,
            Families = memberships.Select(m => new UserFamilyDto
            {
                FamilyId = m.FamilyId,
                FamilyName = m.Family.Name,
                Role = m.Role.ToString()
            }).ToList()
        };
    }

    // ==================== JWT ====================

    private string GenerateJwtToken(ApplicationUser user, Guid? familyId, UserRole? role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new("is_super_admin", user.IsSuperAdmin.ToString().ToLowerInvariant())
        };

        if (familyId.HasValue)
            claims.Add(new Claim("family_id", familyId.Value.ToString()));

        if (role.HasValue)
            claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
