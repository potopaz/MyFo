using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.CashBoxes.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.CashBoxes.Queries;

public class GetCashBoxPermissionsQueryHandler : IRequestHandler<GetCashBoxPermissionsQuery, List<CashBoxMemberPermissionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCashBoxPermissionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CashBoxMemberPermissionDto>> Handle(GetCashBoxPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        // Solo admins pueden ver/gestionar permisos
        var caller = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new ForbiddenException("Miembro no encontrado.");

        if (caller.Role != Domain.Identity.Enums.UserRole.FamilyAdmin)
            throw new ForbiddenException("Solo los administradores pueden gestionar permisos.");

        // Cargar todos los miembros activos de la familia
        var members = await _db.FamilyMembers
            .Where(m => m.IsActive)
            .Select(m => new { m.MemberId, m.DisplayName })
            .ToListAsync(cancellationToken);

        // Cargar permisos actuales para esta caja (records no borrados = tiene permiso)
        var grantedMemberIds = await _db.CashBoxPermissions
            .Where(p => p.CashBoxId == request.CashBoxId)
            .Select(p => p.MemberId)
            .ToListAsync(cancellationToken);

        var grantedSet = grantedMemberIds.ToHashSet();

        return members.Select(m => new CashBoxMemberPermissionDto
        {
            MemberId = m.MemberId,
            DisplayName = m.DisplayName,
            Permission = grantedSet.Contains(m.MemberId) ? "Operate" : null
        }).ToList();
    }
}
