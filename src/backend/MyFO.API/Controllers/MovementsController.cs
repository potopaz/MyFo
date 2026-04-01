using MyFO.Application.Common.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Transactions.Movements.Commands;
using MyFO.Application.Transactions.Movements.DTOs;
using MyFO.Application.Transactions.Movements.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MovementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MovementsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<MovementListItemDto>>> GetAll(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] string? movementType,
        [FromQuery] Guid? subcategoryId,
        [FromQuery] string? description,
        CancellationToken ct)
    {
        var query = new GetMovementsQuery
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            MovementType = movementType,
            SubcategoryId = subcategoryId,
            Description = description,
        };
        return Ok(await _mediator.Send(query, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MovementDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetMovementByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<MovementDto>> Create([FromBody] CreateMovementCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/movements/{result.MovementId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MovementDto>> Update(Guid id, [FromBody] UpdateMovementCommand command, CancellationToken ct)
    {
        command.MovementId = id;
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteMovementCommand(id), ct);
        return NoContent();
    }
}
