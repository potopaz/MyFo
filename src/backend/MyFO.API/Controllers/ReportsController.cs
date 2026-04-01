using MyFO.Application.Common.Mediator;
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

    // ── New report endpoints ───────────────────────────────────────────────────

    [HttpGet("income-expense")]
    public async Task<ActionResult<IncomeExpenseReportDto>> GetIncomeExpense(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string currency,
        [FromQuery] string? categoryId,
        [FromQuery] string? subcategoryId,
        [FromQuery] string? costCenterId,
        [FromQuery] string? isOrdinary,
        CancellationToken ct)
    {
        var q = new GetIncomeExpenseQuery
        {
            From = ParseDate(from),
            To   = ParseDate(to),
            Currency = currency ?? string.Empty,
        };
        if (Guid.TryParse(categoryId, out var catId))    q.CategoryId = catId;
        if (Guid.TryParse(subcategoryId, out var subId)) q.SubcategoryId = subId;
        if (Guid.TryParse(costCenterId, out var ccId))   q.CostCenterId = ccId;
        if (bool.TryParse(isOrdinary, out var ord))      q.IsOrdinary = ord;

        return Ok(await _mediator.Send(q, ct));
    }

    [HttpGet("cashflow")]
    public async Task<ActionResult<CashFlowReportDto>> GetCashFlow(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string currency,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetCashFlowQuery
        {
            From = ParseDate(from),
            To   = ParseDate(to),
            Currency = currency ?? string.Empty,
        }, ct));

    [HttpGet("cards-cc")]
    public async Task<ActionResult<CardsCCReportDto>> GetCardsCC(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string currency,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetCardsCCQuery
        {
            From = ParseDate(from),
            To   = ParseDate(to),
            Currency = currency ?? string.Empty,
        }, ct));

    [HttpGet("patrimony")]
    public async Task<ActionResult<PatrimonyReportDto>> GetPatrimony(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string currency,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetPatrimonyQuery
        {
            From = ParseDate(from),
            To   = ParseDate(to),
            Currency = currency ?? string.Empty,
        }, ct));

    [HttpGet("drilldown")]
    public async Task<ActionResult<DrilldownResultDto>> GetDrilldown(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string currency,
        [FromQuery] string? dimension,
        [FromQuery] string? dimensionValue,
        [FromQuery] string? movementType,
        [FromQuery] string? installmentMonth,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDrilldownQuery
        {
            From = ParseDate(from),
            To   = ParseDate(to),
            Currency = currency ?? string.Empty,
            Dimension = dimension,
            DimensionValue = dimensionValue,
            MovementType = movementType,
            InstallmentMonth = installmentMonth,
            Page = page,
            PageSize = pageSize,
        }, ct));

    private static DateOnly ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return DateOnly.FromDateTime(DateTime.UtcNow);
        return DateOnly.TryParse(value, out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
