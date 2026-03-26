using MediatR;
using MyFO.Application.Accounting.Categories.DTOs;

namespace MyFO.Application.Accounting.Subcategories.Commands;

public class UpdateSubcategoryCommand : IRequest<SubcategoryDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid SubcategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubcategoryType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? SuggestedAccountingType { get; set; }
    public Guid? SuggestedCostCenterId { get; set; }
    public bool? IsOrdinary { get; set; }
    /// <summary>
    /// If set and different from the current category, moves the subcategory to that category.
    /// </summary>
    public Guid? NewCategoryId { get; set; }
}
