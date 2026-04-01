using MyFO.Application.Common.Mediator;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Application.Transactions.FrequentMovements.DTOs;
using MyFO.Domain.Exceptions;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions.Enums;

namespace MyFO.Application.Transactions.FrequentMovements.Queries;

public class GetFrequentMovementsQueryHandler : IRequestHandler<GetFrequentMovementsQuery, List<FrequentMovementListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFrequentMovementsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<FrequentMovementListItemDto>> Handle(GetFrequentMovementsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.FamilyId is null)
            throw new ForbiddenException("No hay familia seleccionada.");

        var frequentMovements = await _db.FrequentMovements
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        if (frequentMovements.Count == 0)
            return [];

        var familyId = _currentUser.FamilyId.Value;

        var cashBoxIds = frequentMovements.Where(f => f.CashBoxId.HasValue).Select(f => f.CashBoxId!.Value).Distinct().ToList();
        var bankAccountIds = frequentMovements.Where(f => f.BankAccountId.HasValue).Select(f => f.BankAccountId!.Value).Distinct().ToList();
        var creditCardIds = frequentMovements.Where(f => f.CreditCardId.HasValue).Select(f => f.CreditCardId!.Value).Distinct().ToList();
        var subcategoryIds = frequentMovements.Select(f => f.SubcategoryId).Distinct().ToList();

        var cashBoxNames = cashBoxIds.Count > 0
            ? await _db.CashBoxes.Where(c => cashBoxIds.Contains(c.CashBoxId)).Select(c => new { c.CashBoxId, c.Name }).ToDictionaryAsync(c => c.CashBoxId, c => c.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var bankAccountNames = bankAccountIds.Count > 0
            ? await _db.BankAccounts.Where(b => bankAccountIds.Contains(b.BankAccountId)).Select(b => new { b.BankAccountId, b.Name }).ToDictionaryAsync(b => b.BankAccountId, b => b.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var creditCardNames = creditCardIds.Count > 0
            ? await _db.CreditCards.Where(c => creditCardIds.Contains(c.CreditCardId)).Select(c => new { c.CreditCardId, c.Name }).ToDictionaryAsync(c => c.CreditCardId, c => c.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var subcategories = await _db.Subcategories
            .Where(s => subcategoryIds.Contains(s.SubcategoryId))
            .Select(s => new { s.SubcategoryId, s.Name, s.CategoryId })
            .ToListAsync(cancellationToken);

        var categoryIds = subcategories.Select(s => s.CategoryId).Distinct().ToList();
        var categoryNames = await _db.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .Select(c => new { c.CategoryId, c.Name })
            .ToDictionaryAsync(c => c.CategoryId, c => c.Name, cancellationToken);

        var subcategoryMap = subcategories.ToDictionary(s => s.SubcategoryId, s => new { s.Name, s.CategoryId });

        return frequentMovements.Select(f =>
        {
            subcategoryMap.TryGetValue(f.SubcategoryId, out var sub);
            var categoryName = sub != null && categoryNames.TryGetValue(sub.CategoryId, out var cn) ? cn : string.Empty;
            string? paymentEntityName = f.PaymentMethodType switch
            {
                PaymentMethodType.CashBox => f.CashBoxId.HasValue && cashBoxNames.TryGetValue(f.CashBoxId.Value, out var n) ? n : null,
                PaymentMethodType.BankAccount => f.BankAccountId.HasValue && bankAccountNames.TryGetValue(f.BankAccountId.Value, out var n) ? n : null,
                _ => f.CreditCardId.HasValue && creditCardNames.TryGetValue(f.CreditCardId.Value, out var n) ? n : null,
            };
            return new FrequentMovementListItemDto
            {
                FrequentMovementId = f.FrequentMovementId,
                Name = f.Name,
                MovementType = f.MovementType.ToString(),
                Amount = f.Amount,
                CurrencyCode = f.CurrencyCode,
                Description = f.Description,
                SubcategoryName = sub?.Name ?? string.Empty,
                CategoryName = categoryName,
                PaymentMethodType = f.PaymentMethodType.ToString(),
                PaymentEntityName = paymentEntityName,
                FrequencyMonths = f.FrequencyMonths,
                LastAppliedAt = f.LastAppliedAt,
                NextDueDate = f.NextDueDate,
                IsActive = f.IsActive,
            };
        }).ToList();
    }
}
