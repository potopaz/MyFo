using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Accounting.FamilyCurrencies.Commands;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;
using MyFO.Application.Accounting.FamilyCurrencies.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FamilyCurrenciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FamilyCurrenciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<FamilyCurrencyDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetFamilyCurrenciesQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<FamilyCurrencyDto>> Add([FromBody] AddFamilyCurrencyCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/familycurrencies/{result.FamilyCurrencyId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FamilyCurrencyDto>> Update(Guid id, [FromBody] UpdateFamilyCurrencyBodyDto body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateFamilyCurrencyCommand(id, body.IsActive), ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFamilyCurrencyCommand(id), ct);
        return NoContent();
    }
}

public class UpdateFamilyCurrencyBodyDto
{
    public bool IsActive { get; set; }
}
