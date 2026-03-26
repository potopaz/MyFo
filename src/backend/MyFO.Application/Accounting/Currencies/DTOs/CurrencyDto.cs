namespace MyFO.Application.Accounting.Currencies.DTOs;

public class CurrencyDto
{
    public Guid CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
}
