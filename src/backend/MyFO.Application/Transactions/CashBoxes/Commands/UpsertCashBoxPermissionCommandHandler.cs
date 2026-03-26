using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.CashBoxes.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public class UpsertCashBoxPermissionCommandHandler : IRequestHandler<UpsertCashBoxPermissionCommand, CashBoxMemberPermissionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpsertCashBoxPermissionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CashBoxMemberPermissionDto> Handle(UpsertCashBoxPermissionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Solo admins pueden gestionar permisos
        var caller = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new ForbiddenException("Miembro no encontrado.");

        if (caller.Role != Domain.Identity.Enums.UserRole.FamilyAdmin)
            throw new ForbiddenException("Solo los administradores pueden gestionar permisos.");

        // Validar que la caja existe
        var cashBoxExists = await _db.CashBoxes.AnyAsync(c => c.CashBoxId == request.CashBoxId, cancellationToken);
        if (!cashBoxExists)
            throw new DomainException("NOT_FOUND", "La caja no existe.");

        // Validar que el miembro existe y pertenece a la familia
        var member = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.MemberId == request.MemberId, cancellationToken)
            ?? throw new DomainException("NOT_FOUND", "El miembro no existe.");

        // Buscar permiso existente (incluyendo soft-deleted para reactivar y evitar error de PK)
        var existing = await _db.CashBoxPermissions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.FamilyId == familyId && p.CashBoxId == request.CashBoxId && p.MemberId == request.MemberId, cancellationToken);

        if (existing is not null)
        {
            // Restaurar si estaba soft-deleted
            existing.DeletedAt = null;
            existing.DeletedBy = null;
        }
        else
        {
            var permission = new CashBoxPermission
            {
                FamilyId = familyId,
                CashBoxId = request.CashBoxId,
                MemberId = request.MemberId
            };
            await _db.CashBoxPermissions.AddAsync(permission, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CashBoxMemberPermissionDto
        {
            MemberId = member.MemberId,
            DisplayName = member.DisplayName,
            Permission = "Operate"
        };
    }
}
