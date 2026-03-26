using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Identity.FamilySettings.Commands;
using MyFO.Application.Identity.FamilySettings.DTOs;
using MyFO.Application.Identity.FamilySettings.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/family-settings")]
public class FamilySettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FamilySettingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<FamilySettingsDto>> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFamilySettingsQuery(), ct);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<FamilySettingsDto>> Update(
        [FromBody] UpdateFamilySettingsCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
