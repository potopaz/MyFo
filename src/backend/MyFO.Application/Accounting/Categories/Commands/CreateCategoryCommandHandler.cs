using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.Categories.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting;
using MyFO.Domain.Accounting.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.Categories.Commands;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Check for existing (including soft-deleted) category with same name
        var existing = await _db.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.Name == request.Name, cancellationToken);

        Category category;

        if (existing is not null)
        {
            if (existing.DeletedAt is null)
                throw new DomainException("DUPLICATE_NAME", $"Ya existe una categoría con el nombre '{request.Name}'.");

            // Reactivate soft-deleted record and update data
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.Icon = request.Icon;
            category = existing;
        }
        else
        {
            category = new Category
            {
                FamilyId = familyId,
                CategoryId = Guid.NewGuid(),
                Name = request.Name,
                Icon = request.Icon
            };
            await _db.Categories.AddAsync(category, cancellationToken);
        }

        if (request.Subcategories is { Count: > 0 })
        {
            foreach (var sub in request.Subcategories)
            {
                if (!Enum.TryParse<SubcategoryType>(sub.SubcategoryType, true, out var subType))
                    throw new DomainException("INVALID_TYPE", $"SubcategoryType inválido: '{sub.SubcategoryType}'. Usar Income, Expense o Both.");

                AccountingType? accountingType = null;
                if (sub.SuggestedAccountingType is not null)
                {
                    if (!Enum.TryParse<AccountingType>(sub.SuggestedAccountingType, true, out var at))
                        throw new DomainException("INVALID_TYPE", $"AccountingType inválido: '{sub.SuggestedAccountingType}'.");
                    accountingType = at;
                }

                category.Subcategories.Add(new Subcategory
                {
                    FamilyId = familyId,
                    SubcategoryId = Guid.NewGuid(),
                    CategoryId = category.CategoryId,
                    Name = sub.Name,
                    SubcategoryType = subType,
                    SuggestedAccountingType = accountingType,
                    SuggestedCostCenterId = sub.SuggestedCostCenterId,
                    IsOrdinary = sub.IsOrdinary
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Icon = category.Icon,
            Subcategories = category.Subcategories.Select(s => new SubcategoryDto
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
