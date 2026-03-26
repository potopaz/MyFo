using MyFO.Application.Auth.DTOs;

namespace MyFO.Application.Auth;

public interface IAuthService
{
    // New auth flow
    Task<CheckEmailResponse> CheckEmailAsync(CheckEmailRequest request, CancellationToken cancellationToken = default);
    Task InitiateRegistrationAsync(InitiateRegistrationRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<SelectFamilyResponse> SelectFamilyAsync(Guid userId, SelectFamilyRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> CreateFamilyAsync(Guid userId, CreateFamilyRequest request, CancellationToken cancellationToken = default);

    // External (social) auth
    Task<AuthResponse> ExternalLoginAsync(string email, string fullName, string provider, string providerKey, CancellationToken cancellationToken = default);

    // Profile
    Task UpdateProfileAsync(string userId, string fullName, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    // Legacy (kept for invitation flow in Phase A)
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterWithInvitationAsync(RegisterWithInvitationRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default);
}
