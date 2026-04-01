using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Accounting.Subcategories.Commands;

public class DeleteSubcategoryCommandHandler : IRequestHandler<DeleteSubcategoryCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteSubcategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteSubcategoryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var entity = await _db.Subcategories
            .FirstOrDefaultAsync(x => x.FamilyId == _currentUser.FamilyId.Value
                                   && x.SubcategoryId == request.SubcategoryId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("Subcategory", request.SubcategoryId);

        var hasMovements = await _db.Movements
            .AnyAsync(m => m.SubcategoryId == request.SubcategoryId, cancellationToken);

        var hasFrequentMovements = await _db.FrequentMovements
            .AnyAsync(fm => fm.SubcategoryId == request.SubcategoryId, cancellationToken);

        if (hasMovements || hasFrequentMovements)
            throw new DomainException("SUBCATEGORY_IN_USE",
                "Esta subcategoría ya fue utilizada en movimientos. No se puede eliminar.");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
