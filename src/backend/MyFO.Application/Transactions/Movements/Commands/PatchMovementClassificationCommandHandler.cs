using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting.Enums;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.Movements.Commands;

public class PatchMovementClassificationCommandHandler : IRequestHandler<PatchMovementClassificationCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PatchMovementClassificationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(PatchMovementClassificationCommand request, CancellationToken cancellationToken)
    {
        var familyId = _currentUser.FamilyId ?? throw new ForbiddenException("No hay familia seleccionada.");

        var movement = await _db.Movements
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.MovementId == request.MovementId, cancellationToken)
            ?? throw new NotFoundException("Movement", request.MovementId);

        if (request.RowVersion != movement.RowVersion)
            throw new ConflictException("CONCURRENT_MODIFICATION",
                "El movimiento fue modificado recientemente. Actualizá la página para ver los cambios.");

        // Validate subcategory
        var subcategory = await _db.Subcategories
            .FirstOrDefaultAsync(s => s.FamilyId == familyId && s.SubcategoryId == request.SubcategoryId, cancellationToken)
            ?? throw new NotFoundException("Subcategory", request.SubcategoryId);

        if (!subcategory.IsActive)
            throw new DomainException("SUBCATEGORY_INACTIVE", "La subcategoría no está activa.");

        var isIncome = movement.MovementType == MovementType.Income;
        var typeOk = subcategory.SubcategoryType switch
        {
            SubcategoryType.Both    => true,
            SubcategoryType.Income  => isIncome,
            SubcategoryType.Expense => !isIncome,
            _                       => false,
        };
        if (!typeOk)
            throw new DomainException("SUBCATEGORY_TYPE_MISMATCH",
                isIncome
                    ? "La subcategoría es de tipo Gasto y no se puede usar en un Ingreso."
                    : "La subcategoría es de tipo Ingreso y no se puede usar en un Gasto.");

        // Validate cost center
        if (request.CostCenterId.HasValue)
        {
            var cc = await _db.CostCenters
                .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.CostCenterId == request.CostCenterId.Value, cancellationToken)
                ?? throw new NotFoundException("CostCenter", request.CostCenterId.Value);

            if (!cc.IsActive)
                throw new DomainException("COST_CENTER_INACTIVE", "El centro de costo no está activo.");
        }

        // Validate accounting type
        if (!string.IsNullOrEmpty(request.AccountingType) && !Enum.TryParse<AccountingType>(request.AccountingType, out _))
            throw new DomainException("INVALID_ACCOUNTING_TYPE", $"Tipo contable no válido: '{request.AccountingType}'.");

        movement.SubcategoryId = request.SubcategoryId;
        movement.AccountingType = request.AccountingType;
        movement.IsOrdinary = request.IsOrdinary;
        movement.CostCenterId = request.CostCenterId;
        movement.RowVersion++;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
