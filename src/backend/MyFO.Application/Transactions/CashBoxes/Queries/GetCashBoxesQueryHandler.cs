using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.CashBoxes.DTOs;
using MyFO.Domain.Identity.Enums;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.CashBoxes.Queries;

public class GetCashBoxesQueryHandler : IRequestHandler<GetCashBoxesQuery, List<CashBoxDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCashBoxesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<CashBoxDto>> Handle(GetCashBoxesQuery request, CancellationToken cancellationToken)
    {
        var member = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.UserId == _currentUser.UserId, cancellationToken);

        var isAdmin = member?.Role == UserRole.FamilyAdmin;
        var memberId = member?.MemberId ?? Guid.Empty;

        IQueryable<Domain.Transactions.CashBox> query = _db.CashBoxes;

        // Admin ve todas las cajas. Miembro solo ve las que tiene permiso (puede operar).
        if (!isAdmin)
        {
            query = query.Where(c => _db.CashBoxPermissions.Any(p => p.CashBoxId == c.CashBoxId && p.MemberId == memberId));
        }

        var cashBoxes = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);

        var operatableCashBoxIds = await _db.CashBoxPermissions
            .Where(p => p.MemberId == memberId)
            .Select(p => p.CashBoxId)
            .ToHashSetAsync(cancellationToken);

        return cashBoxes.Select(c => new CashBoxDto
        {
            CashBoxId = c.CashBoxId,
            Name = c.Name,
            CurrencyCode = c.CurrencyCode,
            InitialBalance = c.InitialBalance,
            Balance = c.Balance,
            IsActive = c.IsActive,
            CanOperate = operatableCashBoxIds.Contains(c.CashBoxId)
        }).ToList();
    }
}
