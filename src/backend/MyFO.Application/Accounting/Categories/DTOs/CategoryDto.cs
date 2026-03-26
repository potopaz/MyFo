namespace MyFO.Application.Accounting.Categories.DTOs;

public class CategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public List<SubcategoryDto> Subcategories { get; set; } = [];
}

public class SubcategoryDto
{
    public Guid SubcategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubcategoryType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? SuggestedAccountingType { get; set; }
    public Guid? SuggestedCostCenterId { get; set; }
    public bool? IsOrdinary { get; set; }
}
