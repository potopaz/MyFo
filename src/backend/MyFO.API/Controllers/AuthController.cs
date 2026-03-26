using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Auth;
using MyFO.Application.Auth.DTOs;

namespace MyFO.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<InitiateRegistrationRequest> _initiateRegistrationValidator;
    private readonly IValidator<CreateFamilyRequest> _createFamilyValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<InitiateRegistrationRequest> initiateRegistrationValidator,
        IValidator<CreateFamilyRequest> createFamilyValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _initiateRegistrationValidator = initiateRegistrationValidator;
        _createFamilyValidator = createFamilyValidator;
    }

    // ==================== NEW AUTH FLOW ====================

    [HttpPost("check-email")]
    public async Task<ActionResult<CheckEmailResponse>> CheckEmail(
        [FromBody] CheckEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { code = "VALIDATION_ERROR", message = "El email es requerido." });

        var response = await _authService.CheckEmailAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("initiate-registration")]
    public async Task<IActionResult> InitiateRegistration(
        [FromBody] InitiateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _initiateRegistrationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                code = "VALIDATION_ERROR",
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        await _authService.InitiateRegistrationAsync(request, cancellationToken);
        return Ok(new { message = "Email de verificación enviado." });
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<AuthResponse>> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { code = "VALIDATION_ERROR", message = "El token es requerido." });

        var response = await _authService.VerifyEmailAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { code = "VALIDATION_ERROR", message = "El email es requerido." });

        await _authService.ForgotPasswordAsync(request, cancellationToken);
        // Always return success to not reveal if email exists
        return Ok(new { message = "Si el email existe, recibirás instrucciones para restablecer tu contraseña." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Todos los campos son requeridos." });

        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new { message = "Contraseña restablecida exitosamente." });
    }

    [Authorize]
    [HttpPost("select-family")]
    public async Task<ActionResult<SelectFamilyResponse>> SelectFamily(
        [FromBody] SelectFamilyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _authService.SelectFamilyAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("create-family")]
    public async Task<ActionResult<AuthResponse>> CreateFamily(
        [FromBody] CreateFamilyRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createFamilyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                code = "VALIDATION_ERROR",
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await _authService.CreateFamilyAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    // ==================== EXTERNAL (SOCIAL) AUTH ====================

    [HttpGet("external-login")]
    public IActionResult ExternalLogin([FromQuery] string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return BadRequest(new { code = "VALIDATION_ERROR", message = "El proveedor es requerido." });

        var redirectUrl = Url.Action(nameof(ExternalCallback), "Auth", null, Request.Scheme);
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items["LoginProvider"] = provider;
        return Challenge(properties, provider);
    }

    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalCallback(CancellationToken cancellationToken)
    {
        var frontendUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["App:FrontendUrl"]
            ?? "http://localhost:5173";

        try
        {
            var result = await HttpContext.AuthenticateAsync("ExternalCookie");
            if (!result.Succeeded || result.Principal is null)
                return Redirect($"{frontendUrl}/auth?error=external_auth_failed");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name)
                    ?? result.Principal.FindFirstValue(ClaimTypes.GivenName)
                    ?? "Usuario";
            var provider = result.Properties?.Items["LoginProvider"] ?? "Unknown";
            var providerKey = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            if (string.IsNullOrWhiteSpace(email))
                return Redirect($"{frontendUrl}/auth?error=no_email");

            // Clean up the external cookie
            await HttpContext.SignOutAsync("ExternalCookie");

            var response = await _authService.ExternalLoginAsync(email, name, provider, providerKey, cancellationToken);

            // Redirect to frontend with auth data as URL params
            var familiesJson = System.Text.Json.JsonSerializer.Serialize(response.Families,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            var callbackUrl = $"{frontendUrl}/auth/external-callback"
                + $"?token={Uri.EscapeDataString(response.Token)}"
                + $"&userId={response.UserId}"
                + $"&email={Uri.EscapeDataString(response.Email)}"
                + $"&fullName={Uri.EscapeDataString(response.FullName)}"
                + $"&isSuperAdmin={response.IsSuperAdmin.ToString().ToLowerInvariant()}"
                + $"&families={Uri.EscapeDataString(familiesJson)}";

            return Redirect(callbackUrl);
        }
        catch (Exception)
        {
            return Redirect($"{frontendUrl}/auth?error=external_auth_failed");
        }
    }

    // ==================== PROFILE ====================

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _authService.UpdateProfileAsync(userId, request.FullName, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return NoContent();
    }

    // ==================== LEGACY (invitation flow) ====================

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                code = "VALIDATION_ERROR",
                errors = validation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }

        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("register-with-invitation")]
    public async Task<ActionResult<AuthResponse>> RegisterWithInvitation(
        [FromBody] RegisterWithInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.InvitationToken))
        {
            return BadRequest(new { code = "VALIDATION_ERROR", message = "Todos los campos son requeridos." });
        }

        var response = await _authService.RegisterWithInvitationAsync(request, cancellationToken);
        return Ok(response);
    }
}
