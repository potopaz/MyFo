using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.CreditCards.StatementLineItems.Commands;
using MyFO.Application.CreditCards.StatementPeriods.Commands;
using MyFO.Application.CreditCards.StatementPeriods.DTOs;
using MyFO.Application.CreditCards.StatementPeriods.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class StatementPeriodsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatementPeriodsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<StatementPeriodDto>>> GetAll(
        [FromQuery] Guid? creditCardId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAllStatementPeriodsQuery(creditCardId, status), ct));

    [HttpGet("by-card/{creditCardId}")]
    public async Task<ActionResult<List<StatementPeriodDto>>> GetByCard(Guid creditCardId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStatementPeriodsQuery(creditCardId), ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<StatementPeriodDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStatementPeriodByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<StatementPeriodDto>> Create([FromBody] CreateStatementPeriodCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/statementperiods/{result.StatementPeriodId}", result);
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<StatementPeriodDto>> Close(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new CloseStatementPeriodCommand(id), ct));

    [HttpPost("{id}/reopen")]
    public async Task<ActionResult<StatementPeriodDto>> Reopen(Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new ReopenStatementPeriodCommand(id), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteStatementPeriodCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("{id}/dates")]
    public async Task<IActionResult> UpdateDates(
        Guid id, [FromBody] UpdateStatementPeriodDatesCommand command, CancellationToken ct)
    {
        command.StatementPeriodId = id;
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // --- Installments ---

    [HttpPatch("installments/{installmentId}/actual-amount")]
    public async Task<IActionResult> UpdateInstallmentActualAmount(
        Guid installmentId, [FromBody] UpdateInstallmentActualAmountCommand command, CancellationToken ct)
    {
        command.CreditCardInstallmentId = installmentId;
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("{id}/installments/{installmentId}/toggle-inclusion")]
    public async Task<IActionResult> ToggleInstallmentInclusion(
        Guid id, Guid installmentId, [FromBody] ToggleInstallmentInclusionCommand command, CancellationToken ct)
    {
        command.StatementPeriodId = id;
        command.CreditCardInstallmentId = installmentId;
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPatch("installments/{installmentId}/bonification-amount")]
    public async Task<IActionResult> UpdateInstallmentBonificationAmount(
        Guid installmentId, [FromBody] UpdateInstallmentBonificationAmountCommand command, CancellationToken ct)
    {
        command.CreditCardInstallmentId = installmentId;
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("{id}/installments/{installmentId}/toggle-bonification")]
    public async Task<IActionResult> ToggleBonificationInclusion(
        Guid id, Guid installmentId, [FromBody] ToggleBonificationInclusionCommand command, CancellationToken ct)
    {
        command.StatementPeriodId = id;
        command.CreditCardInstallmentId = installmentId;
        await _mediator.Send(command, ct);
        return NoContent();
    }

    // --- Line Items ---

    [HttpPost("{id}/line-items")]
    public async Task<ActionResult<StatementLineItemDto>> AddLineItem(Guid id, [FromBody] AddStatementLineItemCommand command, CancellationToken ct)
    {
        command.StatementPeriodId = id;
        var result = await _mediator.Send(command, ct);
        return Created($"/api/statementperiods/{id}", result);
    }

    [HttpDelete("line-items/{lineItemId}")]
    public async Task<IActionResult> DeleteLineItem(Guid lineItemId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteStatementLineItemCommand(lineItemId), ct);
        return NoContent();
    }

}
