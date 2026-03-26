namespace MyFO.Application.Reports.DTOs;

public class PeriodAnalysisDto
{
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Result { get; set; }
    public List<DimensionItemDto> ByCategory { get; set; } = [];
    public List<DimensionItemDto> BySubcategory { get; set; } = [];
    public List<DimensionItemDto> ByCostCenter { get; set; } = [];
    public List<DimensionItemDto> ByCharacter { get; set; } = [];
    public List<DimensionItemDto> ByAccountingType { get; set; } = [];
}

public class DimensionItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}
