using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Accounting.Currencies.Commands;
using MyFO.Application.Accounting.Currencies.DTOs;
using MyFO.Application.Accounting.Currencies.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CurrenciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurrenciesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<CurrencyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrenciesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyDto>> Create(
        [FromBody] CreateCurrencyCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Created($"/api/currencies/{result.CurrencyId}", result);
    }
}
