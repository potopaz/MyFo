using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.CreditCards.CreditCardPayments.Commands;
using MyFO.Application.CreditCards.CreditCardPayments.DTOs;
using MyFO.Application.CreditCards.CreditCardPayments.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CreditCardPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CreditCardPaymentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<CreditCardPaymentDto>>> GetAll(
        [FromQuery] Guid? creditCardId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCreditCardPaymentsQuery(creditCardId), ct));

    [HttpPost]
    public async Task<ActionResult<CreditCardPaymentDto>> Create(
        [FromBody] CreateCreditCardPaymentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/creditcardpayments/{result.CreditCardPaymentId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CreditCardPaymentDto>> Update(
        Guid id, [FromBody] UpdateCreditCardPaymentCommand command, CancellationToken ct)
    {
        command.CreditCardPaymentId = id;
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCreditCardPaymentCommand(id), ct);
        return NoContent();
    }
}
