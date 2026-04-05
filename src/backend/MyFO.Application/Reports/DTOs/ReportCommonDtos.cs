namespace MyFO.Application.Reports.DTOs;

public class NameAmountDto
{
    public string Name { get; set; } = string.Empty;
    public string? Id { get; set; }
    public decimal Amount { get; set; }
}

public class TimePointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TimeSeriesMultiDto
{
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, decimal> Values { get; set; } = [];
}

public class OrdVsExtraDto
{
    public decimal Ordinary { get; set; }
    public decimal Extraordinary { get; set; }
    public decimal Unspecified { get; set; }
}

public class DrilldownMovementDto
{
    public Guid MovementId { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public Guid SubcategoryId { get; set; }
    public string SubcategoryName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Guid? CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    public bool? IsOrdinary { get; set; }
    public string? AccountingType { get; set; }
    public int RowVersion { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
}

public class DrilldownResultDto
{
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Signed net: income positive, expense negative</summary>
    public decimal NetAmount { get; set; }
    public List<DrilldownMovementDto> Items { get; set; } = [];
}
