using MyFO.Application.Common.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Transactions.BankAccounts.Commands;
using MyFO.Application.Transactions.BankAccounts.DTOs;
using MyFO.Application.Transactions.BankAccounts.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BankAccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BankAccountsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<BankAccountDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetBankAccountsQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create([FromBody] CreateBankAccountCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/bankaccounts/{result.BankAccountId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BankAccountDto>> Update(Guid id, [FromBody] UpdateBankAccountCommand command, CancellationToken ct)
    {
        command.BankAccountId = id;
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteBankAccountCommand(id), ct);
        return NoContent();
    }
}
