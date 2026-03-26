using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFO.Application.Common.Interfaces;

namespace MyFO.API.Controllers;

[ApiController]
[Authorize]
[Route("api/exchange-rates")]
public class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public ExchangeRatesController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    /// <summary>
    /// Returns the exchange rate from baseCurrency to targetCurrency for the given date.
    /// Uses a DB cache; if not cached, fetches from the external provider.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ExchangeRateResponse>> GetRate(
        [FromQuery] string base_currency,
        [FromQuery] string target_currency,
        [FromQuery] string date,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(base_currency) || string.IsNullOrWhiteSpace(target_currency))
            return BadRequest(new { message = "base_currency y target_currency son requeridos." });

        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest(new { message = "Formato de fecha inválido. Use YYYY-MM-DD." });

        var rate = await _exchangeRateService.GetRateAsync(base_currency, target_currency, parsedDate, cancellationToken);

        if (rate is null)
            return StatusCode(503, new { message = "No se pudo obtener la cotización. Intente más tarde." });

        return Ok(new ExchangeRateResponse(rate.Value));
    }
}

public record ExchangeRateResponse(decimal Rate);
