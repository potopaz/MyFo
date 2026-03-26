using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.Categories.DTOs;
using MyFO.Application.Common.Interfaces;

namespace MyFO.Application.Accounting.Categories.Queries;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCategoriesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Categories
            .Include(c => c.Subcategories.Where(s => s.DeletedAt == null))
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Icon = c.Icon,
                Subcategories = c.Subcategories
                    .OrderBy(s => s.Name)
                    .Select(s => new SubcategoryDto
                    {
                        SubcategoryId = s.SubcategoryId,
                        Name = s.Name,
                        SubcategoryType = s.SubcategoryType.ToString(),
                        IsActive = s.IsActive,
                        SuggestedAccountingType = s.SuggestedAccountingType != null ? s.SuggestedAccountingType.ToString() : null,
                        SuggestedCostCenterId = s.SuggestedCostCenterId,
                        IsOrdinary = s.IsOrdinary
                    }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
