using MediatR;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.FrequentMovements.Commands;
using MyFO.Application.Transactions.FrequentMovements.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;

namespace MyFO.Application.Transactions.FrequentMovements.Queries;

public class GetFrequentMovementByIdQueryHandler : IRequestHandler<GetFrequentMovementByIdQuery, FrequentMovementDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFrequentMovementByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FrequentMovementDto> Handle(GetFrequentMovementByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        var entity = await _db.FrequentMovements
            .FirstOrDefaultAsync(f => f.FamilyId == familyId && f.FrequentMovementId == request.FrequentMovementId, cancellationToken)
            ?? throw new NotFoundException("FrequentMovement", request.FrequentMovementId);

        var userIds = new[] { (Guid?)entity.CreatedBy, entity.ModifiedBy }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var nameByUserId = await _db.FamilyMembers
            .Where(m => userIds.Contains(m.UserId))
            .Select(m => new { m.UserId, m.DisplayName })
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result.GroupBy(m => m.UserId).ToDictionary(g => g.Key, g => g.First().DisplayName));

        var dto = CreateFrequentMovementCommandHandler.MapToDto(entity);
        dto.CreatedAt = entity.CreatedAt;
        dto.CreatedByName = nameByUserId.GetValueOrDefault(entity.CreatedBy);
        dto.ModifiedAt = entity.ModifiedAt;
        dto.ModifiedByName = entity.ModifiedBy.HasValue ? nameByUserId.GetValueOrDefault(entity.ModifiedBy.Value) : null;
        return dto;
    }
}
