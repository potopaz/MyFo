using MyFO.Application.Common.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Admin.Commands;
using MyFO.Application.Admin.DTOs;
using MyFO.Application.Admin.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize(Policy = "SuperAdmin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("families")]
    public async Task<ActionResult<List<AdminFamilyListItemDto>>> GetFamilies(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAdminFamiliesQuery(), ct));

    [HttpGet("families/{id:guid}")]
    public async Task<ActionResult<AdminFamilyDetailDto>> GetFamily(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAdminFamilyDetailQuery { FamilyId = id }, ct));

    [HttpPut("families/{id:guid}/config")]
    public async Task<ActionResult<AdminFamilyDetailDto>> UpdateConfig(
        Guid id,
        [FromBody] UpdateFamilyAdminConfigCommand command,
        CancellationToken ct)
    {
        command.FamilyId = id;
        return Ok(await _mediator.Send(command, ct));
    }
}
