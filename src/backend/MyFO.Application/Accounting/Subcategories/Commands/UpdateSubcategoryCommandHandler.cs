using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Accounting.Categories.DTOs;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.Subcategories.Commands;

public class UpdateSubcategoryCommandHandler : IRequestHandler<UpdateSubcategoryCommand, SubcategoryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateSubcategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SubcategoryDto> Handle(UpdateSubcategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.Subcategories
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.SubcategoryId == request.SubcategoryId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("Subcategory", request.SubcategoryId);

        if (!Enum.TryParse<SubcategoryType>(request.SubcategoryType, true, out var subType))
            throw new DomainException("INVALID_TYPE", $"SubcategoryType invalido: '{request.SubcategoryType}'. Usar Income, Expense o Both.");

        AccountingType? accountingType = null;
        if (request.SuggestedAccountingType is not null)
        {
            if (!Enum.TryParse<AccountingType>(request.SuggestedAccountingType, true, out var at))
                throw new DomainException("INVALID_TYPE", $"AccountingType invalido: '{request.SuggestedAccountingType}'.");
            accountingType = at;
        }

        // Move to another category if requested
        if (request.NewCategoryId.HasValue && request.NewCategoryId.Value != entity.CategoryId)
        {
            var targetCategoryExists = await _db.Categories
                .AnyAsync(c => c.FamilyId == _currentUser.FamilyId.Value && c.CategoryId == request.NewCategoryId.Value, cancellationToken);

            if (!targetCategoryExists)
                throw new NotFoundException("Category", request.NewCategoryId.Value);

            // Check for active subcategory with same name in target
            var activeConflict = await _db.Subcategories
                .AnyAsync(s => s.FamilyId == _currentUser.FamilyId.Value
                            && s.CategoryId == request.NewCategoryId.Value
                            && s.Name == request.Name
                            && s.SubcategoryId != entity.SubcategoryId, cancellationToken);

            if (activeConflict)
                throw new DomainException("DUPLICATE_NAME",
                    $"Ya existe una subcategoría activa con el nombre '{request.Name}' en la categoría destino.");

            // Check for soft-deleted subcategory with same name in target → hard delete it
            var deletedConflict = await _db.Subcategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.FamilyId == _currentUser.FamilyId.Value
                                       && s.CategoryId == request.NewCategoryId.Value
                                       && s.Name == request.Name
                                       && s.DeletedAt != null, cancellationToken);

            if (deletedConflict is not null)
                _db.Subcategories.Remove(deletedConflict);

            entity.CategoryId = request.NewCategoryId.Value;
        }

        entity.Name = request.Name;
        entity.IsActive = request.IsActive;
        entity.SubcategoryType = subType;
        entity.SuggestedAccountingType = accountingType;
        entity.SuggestedCostCenterId = request.SuggestedCostCenterId;
        entity.IsOrdinary = request.IsOrdinary;

        await _db.SaveChangesAsync(cancellationToken);

        return new SubcategoryDto
        {
            SubcategoryId = entity.SubcategoryId,
            Name = entity.Name,
            SubcategoryType = entity.SubcategoryType.ToString(),
            IsActive = entity.IsActive,
            SuggestedAccountingType = entity.SuggestedAccountingType?.ToString(),
            SuggestedCostCenterId = entity.SuggestedCostCenterId,
            IsOrdinary = entity.IsOrdinary
        };
    }
}
