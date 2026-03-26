using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.CreditCards.Commands;
using MyFO.Application.CreditCards.DTOs;
using MyFO.Application.CreditCards.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CreditCardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CreditCardsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<CreditCardDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCreditCardsQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<CreditCardDto>> Create([FromBody] CreateCreditCardCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Created($"/api/creditcards/{result.CreditCardId}", result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CreditCardDto>> Update(Guid id, [FromBody] UpdateCreditCardCommand command, CancellationToken ct)
    {
        command.CreditCardId = id;
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCreditCardCommand(id), ct);
        return NoContent();
    }

    // ── Members ─────────────────────────────────────────────────────────────────

    [HttpPost("{id}/members")]
    public async Task<ActionResult<CreditCardMemberDto>> AddMember(Guid id, [FromBody] AddCreditCardMemberCommand command, CancellationToken ct)
    {
        command.CreditCardId = id;
        var result = await _mediator.Send(command, ct);
        return Created($"/api/creditcards/{id}/members/{result.CreditCardMemberId}", result);
    }

    [HttpPut("{id}/members/{memberId}")]
    public async Task<ActionResult<CreditCardMemberDto>> UpdateMember(Guid id, Guid memberId, [FromBody] UpdateCreditCardMemberCommand command, CancellationToken ct)
    {
        command.CreditCardId = id;
        command.CreditCardMemberId = memberId;
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id}/members/{memberId}")]
    public async Task<IActionResult> DeleteMember(Guid id, Guid memberId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCreditCardMemberCommand(id, memberId), ct);
        return NoContent();
    }
}
