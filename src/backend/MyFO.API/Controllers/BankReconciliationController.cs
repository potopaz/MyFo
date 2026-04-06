using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.BankAccounts.Reconciliation.Commands;
using MyFO.Application.Transactions.BankAccounts.Reconciliation.DTOs;
using MyFO.Application.Transactions.BankAccounts.Reconciliation.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/bank-reconciliation")]
public class BankReconciliationController : ControllerBase
{
    private readonly IMediator _mediator;

    public BankReconciliationController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{bankAccountId}")]
    public async Task<ActionResult<BankReconciliationDto>> Get(
        Guid bankAccountId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetBankReconciliationQuery(bankAccountId, from, to), ct));

    [HttpPatch("{bankAccountId}/movement-payment/{paymentId}")]
    public async Task<ActionResult<bool>> ToggleMovementPayment(
        Guid bankAccountId, Guid paymentId,
        [FromBody] ToggleReconcileRequest body,
        CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleReconcileMovementPaymentCommand(bankAccountId, paymentId, body.IsReconciled), ct));

    [HttpPatch("{bankAccountId}/transfer/{transferId}")]
    public async Task<ActionResult<bool>> ToggleTransfer(
        Guid bankAccountId, Guid transferId,
        [FromBody] ToggleReconcileRequest body,
        CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleReconcileTransferCommand(bankAccountId, transferId, body.IsReconciled), ct));

    [HttpPatch("{bankAccountId}/credit-card-payment/{paymentId}")]
    public async Task<ActionResult<bool>> ToggleCreditCardPayment(
        Guid bankAccountId, Guid paymentId,
        [FromBody] ToggleReconcileRequest body,
        CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleReconcileCreditCardPaymentCommand(bankAccountId, paymentId, body.IsReconciled), ct));

    [HttpPatch("{bankAccountId}/initial-balance")]
    public async Task<ActionResult<bool>> ToggleInitialBalance(
        Guid bankAccountId,
        [FromBody] ToggleReconcileRequest body,
        CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleReconcileInitialBalanceCommand(bankAccountId, body.IsReconciled), ct));
}

public class ToggleReconcileRequest
{
    public bool IsReconciled { get; set; }
}
