using MediatR;
using MyFO.Application.Accounting.Categories.DTOs;

namespace MyFO.Application.Accounting.Categories.Commands;

public record CreateCategoryCommand(
    string Name,
    string? Icon,
    List<CreateSubcategoryItem>? Subcategories
) : IRequest<CategoryDto>;

public class CreateSubcategoryItem
{
    public string Name { get; set; } = string.Empty;
    public string SubcategoryType { get; set; } = string.Empty;
    public string? SuggestedAccountingType { get; set; }
    public Guid? SuggestedCostCenterId { get; set; }
    public bool? IsOrdinary { get; set; }
}
