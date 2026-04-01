using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.CashBoxes.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions;

namespace MyFO.Application.Transactions.CashBoxes.Commands;

public class CreateCashBoxCommandHandler : IRequestHandler<CreateCashBoxCommand, CashBoxDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCashBoxCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CashBoxDto> Handle(CreateCashBoxCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var familyId = _currentUser.FamilyId.Value;

        // Check for existing (including soft-deleted) with same name
        var existing = await _db.CashBoxes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.FamilyId == familyId && c.Name == request.Name, cancellationToken);

        CashBox cashBox;

        if (existing is not null)
        {
            if (existing.DeletedAt is null)
                throw new DomainException("DUPLICATE_NAME", $"Ya existe una caja con el nombre '{request.Name}'.");

            // Reactivate soft-deleted record and update data
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.CurrencyCode = request.CurrencyCode.ToUpperInvariant();
            existing.InitialBalance = request.InitialBalance;
            existing.Balance = request.InitialBalance;
            existing.IsActive = true;
            cashBox = existing;
        }
        else
        {
            cashBox = new CashBox
            {
                FamilyId = familyId,
                CashBoxId = Guid.NewGuid(),
                Name = request.Name,
                CurrencyCode = request.CurrencyCode.ToUpperInvariant(),
                InitialBalance = request.InitialBalance,
                Balance = request.InitialBalance
            };
            await _db.CashBoxes.AddAsync(cashBox, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CashBoxDto
        {
            CashBoxId = cashBox.CashBoxId,
            Name = cashBox.Name,
            CurrencyCode = cashBox.CurrencyCode,
            InitialBalance = cashBox.InitialBalance,
            Balance = cashBox.Balance,
            IsActive = cashBox.IsActive
        };
    }
}
