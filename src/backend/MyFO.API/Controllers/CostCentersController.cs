using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Accounting.CostCenters.Commands;
using MyFO.Application.Accounting.CostCenters.DTOs;
using MyFO.Application.Accounting.CostCenters.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CostCentersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CostCentersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<CostCenterDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCostCentersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CostCenterDto>> Create(
        [FromBody] CreateCostCenterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/costcenters/{result.CostCenterId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CostCenterDto>> Update(Guid id, [FromBody] UpdateCostCenterCommand command, CancellationToken cancellationToken)
    {
        command.CostCenterId = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCostCenterCommand(id), cancellationToken);
        return NoContent();
    }
}
