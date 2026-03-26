using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Reports.DTOs;
using MyFO.Application.Reports.Queries;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboard(
        [FromQuery] string currency,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetDashboardSummaryQuery { Currency = currency ?? string.Empty }, ct));

    [HttpGet("disponibilidades")]
    public async Task<ActionResult<DisponibilidadesDto>> GetDisponibilidades(
        [FromQuery] string currency,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetDisponibilidadesQuery { Currency = currency ?? string.Empty }, ct));

    [HttpGet("analysis")]
    public async Task<ActionResult<PeriodAnalysisDto>> GetAnalysis(
        [FromQuery] string period,
        [FromQuery] string currency,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetPeriodAnalysisQuery
        {
            Period = period ?? "mes-actual",
            Currency = currency ?? string.Empty,
        }, ct));
}
