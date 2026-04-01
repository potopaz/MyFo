using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public class RevokeCashBoxPermissionCommandHandler : IRequestHandler<RevokeCashBoxPermissionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RevokeCashBoxPermissionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RevokeCashBoxPermissionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        // Solo admins pueden revocar permisos
        var caller = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new ForbiddenException("Miembro no encontrado.");

        if (caller.Role != Domain.Identity.Enums.UserRole.FamilyAdmin)
            throw new ForbiddenException("Solo los administradores pueden gestionar permisos.");

        var permission = await _db.CashBoxPermissions
            .FirstOrDefaultAsync(p => p.CashBoxId == request.CashBoxId && p.MemberId == request.MemberId, cancellationToken);

        if (permission is null) return; // Ya no existe, idempotente

        permission.DeletedAt = DateTime.UtcNow;
        permission.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
