using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.Categories.DTOs;

namespace MyFO.Application.Accounting.Subcategories.Commands;

public class CreateSubcategoryCommand : IRequest<SubcategoryDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubcategoryType { get; set; } = string.Empty;
    public string? SuggestedAccountingType { get; set; }
    public Guid? SuggestedCostCenterId { get; set; }
    public bool? IsOrdinary { get; set; }
}
