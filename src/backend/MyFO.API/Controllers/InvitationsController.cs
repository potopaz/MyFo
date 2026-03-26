using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Auth;
using MyFO.Application.Auth.DTOs;
using MyFO.Application.Invitations.Commands;
using MyFO.Application.Invitations.DTOs;
using MyFO.Application.Invitations.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;

    public InvitationsController(IMediator mediator, IAuthService authService)
    {
        _mediator = mediator;
        _authService = authService;
    }

    /// <summary>
    /// POST /api/invitations
    /// Creates an invitation token for the current family.
    /// Requires FamilyAdmin role.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CreateInvitationResponse>> Create([FromBody] CreateInvitationCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/invitations/{token}
    /// Returns invitation info (family name, inviter, validity).
    /// Public endpoint — no authentication required.
    /// </summary>
    [HttpGet("{token}")]
    public async Task<ActionResult<InvitationInfoDto>> GetInfo(string token, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvitationInfoQuery(token), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/invitations/{token}/accept
    /// Accepts an invitation for the currently authenticated user.
    /// The user joins the invited family; a new JWT is returned.
    /// </summary>
    [Authorize]
    [HttpPost("{token}/accept")]
    public async Task<ActionResult<AuthResponse>> Accept(string token, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.AcceptInvitationAsync(token, userId, cancellationToken);
        return Ok(result);
    }
}
