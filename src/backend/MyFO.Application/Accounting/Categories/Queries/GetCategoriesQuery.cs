using MediatR;
using MyFO.Application.Accounting.Categories.DTOs;

namespace MyFO.Application.Accounting.Categories.Queries;

/// <summary>
/// Query to get all categories (with subcategories) for the current tenant.
/// No parameters needed — the tenant filter is applied automatically by EF Core.
/// </summary>
public record GetCategoriesQuery : IRequest<List<CategoryDto>>;
