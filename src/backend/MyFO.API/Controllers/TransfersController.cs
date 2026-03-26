using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Transactions.Transfers.Commands;
using MyFO.Application.Transactions.Transfers.DTOs;
using MyFO.Application.Transactions.Transfers.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransfersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<TransferListItemDto>>> GetAll(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] string? status,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetTransfersQuery(dateFrom, dateTo, status), ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<TransferDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetTransferByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<TransferDto>> Create([FromBody] CreateTransferCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/transfers/{result.TransferId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransferDto>> Update(Guid id, [FromBody] UpdateTransferCommand command, CancellationToken ct)
    {
        command.TransferId = id;
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteTransferCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ConfirmTransferCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectTransferCommand command, CancellationToken ct)
    {
        command.TransferId = id;
        await _mediator.Send(command, ct);
        return NoContent();
    }
}
