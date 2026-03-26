namespace MyFO.Application.Accounting.FamilyCurrencies.DTOs;

public class FamilyCurrencyDto
{
    public Guid FamilyCurrencyId { get; set; }
    public Guid CurrencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
}
