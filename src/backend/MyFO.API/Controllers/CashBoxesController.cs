using MyFO.Application.Common.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Transactions.CashBoxes.Commands;
using MyFO.Application.Transactions.CashBoxes.DTOs;
using MyFO.Application.Transactions.CashBoxes.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CashBoxesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashBoxesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<CashBoxDto>>> GetAll([FromQuery] bool includeAll = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCashBoxesQuery(includeAll), ct));

    [HttpPost]
    public async Task<ActionResult<CashBoxDto>> Create([FromBody] CreateCashBoxCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/cashboxes/{result.CashBoxId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CashBoxDto>> Update(Guid id, [FromBody] UpdateCashBoxCommand command, CancellationToken ct)
    {
        command.CashBoxId = id;
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCashBoxCommand(id), ct);
        return NoContent();
    }

    // --- Permisos ---

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<List<CashBoxMemberPermissionDto>>> GetPermissions(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCashBoxPermissionsQuery(id), ct));

    [HttpPut("{id}/permissions/{memberId}")]
    public async Task<ActionResult<CashBoxMemberPermissionDto>> UpsertPermission(
        Guid id, Guid memberId, [FromBody] UpsertCashBoxPermissionCommand command, CancellationToken ct)
    {
        command.CashBoxId = id;
        command.MemberId = memberId;
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id}/permissions/{memberId}")]
    public async Task<IActionResult> RevokePermission(Guid id, Guid memberId, CancellationToken ct)
    {
        await _mediator.Send(new RevokeCashBoxPermissionCommand(id, memberId), ct);
        return NoContent();
    }
}
