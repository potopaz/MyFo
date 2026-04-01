using MyFO.Application.Common.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Transactions.FrequentMovements.Commands;
using MyFO.Application.Transactions.FrequentMovements.DTOs;
using MyFO.Application.Transactions.FrequentMovements.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/frequent-movements")]
public class FrequentMovementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FrequentMovementsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<FrequentMovementListItemDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetFrequentMovementsQuery(), ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<FrequentMovementDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetFrequentMovementByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<FrequentMovementDto>> Create(
        [FromBody] CreateFrequentMovementCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/frequent-movements/{result.FrequentMovementId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FrequentMovementDto>> Update(
        Guid id, [FromBody] UpdateFrequentMovementCommand command, CancellationToken ct)
    {
        command.FrequentMovementId = id;
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFrequentMovementCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id}/apply")]
    public async Task<IActionResult> Apply(Guid id, [FromBody] ApplyFrequentMovementBody body, CancellationToken ct)
    {
        await _mediator.Send(new ApplyFrequentMovementCommand(id, body.MovementDate), ct);
        return NoContent();
    }
}

public record ApplyFrequentMovementBody(DateOnly MovementDate);
