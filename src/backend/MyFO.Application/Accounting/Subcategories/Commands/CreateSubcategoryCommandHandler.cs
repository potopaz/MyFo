using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.Categories.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting;
using MyFO.Domain.Accounting.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.Subcategories.Commands;

public class CreateSubcategoryCommandHandler : IRequestHandler<CreateSubcategoryCommand, SubcategoryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateSubcategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SubcategoryDto> Handle(CreateSubcategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var categoryExists = await _db.Categories
            .AnyAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                        && x.CategoryId == request.CategoryId, cancellationToken);

        if (!categoryExists)
            throw new NotFoundException("Category", request.CategoryId);

        if (!Enum.TryParse<SubcategoryType>(request.SubcategoryType, true, out var subType))
            throw new DomainException("INVALID_TYPE", $"SubcategoryType invalido: '{request.SubcategoryType}'. Usar Income, Expense o Both.");

        AccountingType? accountingType = null;
        if (request.SuggestedAccountingType is not null)
        {
            if (!Enum.TryParse<AccountingType>(request.SuggestedAccountingType, true, out var at))
                throw new DomainException("INVALID_TYPE", $"AccountingType invalido: '{request.SuggestedAccountingType}'.");
            accountingType = at;
        }

        var familyId = _currentUser.FamilyId.Value;

        // Check for existing (including soft-deleted) subcategory with same name in same category
        var existing = await _db.Subcategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.FamilyId == familyId
                                   && s.CategoryId == request.CategoryId
                                   && s.Name == request.Name, cancellationToken);

        Subcategory entity;

        if (existing is not null)
        {
            if (existing.DeletedAt is null)
                throw new DomainException("DUPLICATE_NAME", $"Ya existe una subcategoría con el nombre '{request.Name}' en esta categoría.");

            // Reactivate soft-deleted record and update data
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.SubcategoryType = subType;
            existing.SuggestedAccountingType = accountingType;
            existing.SuggestedCostCenterId = request.SuggestedCostCenterId;
            existing.IsOrdinary = request.IsOrdinary;
            entity = existing;
        }
        else
        {
            entity = new Subcategory
            {
                FamilyId = familyId,
                SubcategoryId = Guid.NewGuid(),
                CategoryId = request.CategoryId,
                Name = request.Name,
                SubcategoryType = subType,
                SuggestedAccountingType = accountingType,
                SuggestedCostCenterId = request.SuggestedCostCenterId,
                IsOrdinary = request.IsOrdinary
            };
            await _db.Subcategories.AddAsync(entity, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SubcategoryDto
        {
            SubcategoryId = entity.SubcategoryId,
            Name = entity.Name,
            SubcategoryType = entity.SubcategoryType.ToString(),
            SuggestedAccountingType = entity.SuggestedAccountingType?.ToString(),
            SuggestedCostCenterId = entity.SuggestedCostCenterId,
            IsOrdinary = entity.IsOrdinary
        };
    }
}
