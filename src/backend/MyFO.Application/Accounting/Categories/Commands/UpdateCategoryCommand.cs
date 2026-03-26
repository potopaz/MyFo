using MediatR;
using MyFO.Application.Accounting.Categories.DTOs;

namespace MyFO.Application.Accounting.Categories.Commands;

public class UpdateCategoryCommand : IRequest<CategoryDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
}
