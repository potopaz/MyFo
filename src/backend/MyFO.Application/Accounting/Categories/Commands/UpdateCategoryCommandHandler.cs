using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.Categories.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.Categories.Commands;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.Categories
            .Include(c => c.Subcategories)
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.CategoryId == request.CategoryId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("Category", request.CategoryId);

        entity.Name = request.Name;
        entity.Icon = request.Icon;

        await _db.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            CategoryId = entity.CategoryId,
            Name = entity.Name,
            Icon = entity.Icon,
            Subcategories = entity.Subcategories.Select(s => new SubcategoryDto
            {
                SubcategoryId = s.SubcategoryId,
                Name = s.Name,
                SubcategoryType = s.SubcategoryType.ToString(),
                SuggestedAccountingType = s.SuggestedAccountingType?.ToString(),
                SuggestedCostCenterId = s.SuggestedCostCenterId,
                IsOrdinary = s.IsOrdinary
            }).ToList()
        };
    }
}
